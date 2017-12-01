using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Cognitive.Skills;
using System.Drawing;
using System;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.ProjectOxford.EntityLinking;
using Microsoft.ProjectOxford.EntityLinking.Contract;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.ProjectOxford.Vision;



namespace EnricherFunction
{
    public static class EnrichFunction
    {
        static EnrichFunction()
        {
            Init();
        }

        static ImageStore blobContainer;
        static Vision visionClient;
        static HttpClient httpClient = new HttpClient();
        static ISearchIndexClient indexClient;
        static EntityLinkingServiceClient linkedEntityClient;
        static AnnotationStore cosmosDb;

        static void Init()
        {
            if (blobContainer == null)
            {
                // init the blob client
                blobContainer = new ImageStore($"DefaultEndpointsProtocol=https;AccountName={Config.IMAGE_AZURE_STORAGE_ACCOUNT_NAME};AccountKey={Config.IMAGE_BLOB_STORAGE_ACCOUNT_KEY};EndpointSuffix=core.windows.net", Config.IMAGE_BLOB_STORAGE_CONTAINER);
                visionClient = new Vision(Config.VISION_API_KEY);
                var serviceClient = new SearchServiceClient(Config.AZURE_SEARCH_SERVICE_NAME, new SearchCredentials(Config.AZURE_SEARCH_ADMIN_KEY));
                indexClient = serviceClient.Indexes.GetClient(Config.AZURE_SEARCH_INDEX_NAME);
                linkedEntityClient = new EntityLinkingServiceClient(Config.ENTITY_LINKING_API_KEY);
                cosmosDb = new AnnotationStore(Config.COSMOSDB_SERVICE_NAME, Config.COSMOSDB_API_KEY);
            }
        }

        private static Task<EntityLink[]> DetectCIACryptonyms(string txt)
        {
            throw new NotImplementedException();
        }

        private static Task<EntityLink[]> GetLinkedEntitiesAsync(params string[] txts)
        {
            var txt = string.Join(Environment.NewLine, txts);
            if (!string.IsNullOrWhiteSpace(txt))
                return Task.FromResult<EntityLink[]>(null);

            // truncate each page to 10k charactors
            if (txt.Length > 10000)
                txt = txt.Substring(0, 10000);

            return linkedEntityClient.LinkAsync(txt);
        }


        public static SkillSet<PageImage> CreateCognitiveSkillSet()
        {
            var skillSet = SkillSet<PageImage>.Create("page", page => page.Id);

            // prepare the image
            var resizedImage = skillSet.AddSkill("resized-image",
                page => page.GetImage().ResizeFit(2000, 2000).CorrectOrientation().UploadMedia(blobContainer),
                skillSet.Input);

            // Run OCR on the image using the Vision API
            var cogOcr = skillSet.AddSkill("ocr-result",
                imgRef => visionClient.RecognizeTextAsync(imgRef.Url),
                resizedImage);

            // extract text from handwriting
            var handwriting = skillSet.AddSkill("ocr-handwriting",
                imgRef => visionClient.GetHandwritingTextAsync(imgRef.Url),
                resizedImage);

            // Get image descriptions for photos using the computer vision
            var vision = skillSet.AddSkill("computer-vision",
                imgRef => visionClient.AnalyzeImageAsync(imgRef.Url, new[] { VisualFeature.Tags, VisualFeature.Description }),
                resizedImage);

            // extract entities linked to wikipedia using the Entity Linking Service
            var linkedEntities = skillSet.AddSkill("linked-entities",
                (ocr, hw, vis) => GetLinkedEntitiesAsync(ocr.Text, hw.Text, vis.Description.Captions[0].Text),
                cogOcr, handwriting, vision);

            // combine the data as an annotated document
            var pageContent = skillSet.AddSkill("cia-cryptonyms",
                ocr => DetectCIACryptonyms(ocr.Text),
                cogOcr);

            return skillSet;
        }


        public static async Task Run(Stream blobStream, string name, TraceWriter log)
        {
            Init();
            log.Info($"Processing blob:{name}");

            // parse the document to extract images
            IEnumerable<PageImage> pages = DocumentParser.Parse(blobStream).Pages;

            // create and apply the skill set to create annotations
            SkillSet<PageImage> skillSet = CreateCognitiveSkillSet();
            var annotations = await skillSet.ApplyAsync(pages);

            // Commit them to Cosmos DB to be used by full corpus skills such as Topics
            await cosmosDb.SaveAsync(annotations);

            // index the annotated document with azure search
            var searchDocument = new SearchDocument(name)
            {
                Metadata = annotations.Metadata,
                Text = annotations.Text,
                LinkedEntities = annotations.Get<EntityLink[]>("linked-entities")
                     .GroupBy(l => l.Name)
                     .OrderByDescending(g => g.Max(l => l.Score))
                     .Select(l => l.Key)
                     .ToList(),
            };
            var batch = IndexBatch.MergeOrUpload(new[] { searchDocument });
            var result = await indexClient.Documents.IndexAsync(batch);

            if (!result.Results[0].Succeeded)
                log.Error($"index failed for {name}: {result.Results[0].ErrorMessage}");
        }
    }
}

