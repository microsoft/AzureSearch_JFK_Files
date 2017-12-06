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
using System.Reflection;

namespace EnricherFunction
{
    public static class EnrichFunction
    {
        static ImageStore blobContainer;
        static Vision visionClient;
        static HttpClient httpClient = new HttpClient();
        static ISearchIndexClient indexClient;
        static EntityLinkingServiceClient linkedEntityClient;
        static AnnotationStore cosmosDb;
        static Dictionary<string, string> cryptonymns;

        static EnrichFunction()
        {
            blobContainer = new ImageStore($"DefaultEndpointsProtocol=https;AccountName={Config.IMAGE_AZURE_STORAGE_ACCOUNT_NAME};AccountKey={Config.IMAGE_BLOB_STORAGE_ACCOUNT_KEY};EndpointSuffix=core.windows.net", Config.IMAGE_BLOB_STORAGE_CONTAINER);
            visionClient = new Vision(Config.VISION_API_KEY);
            var serviceClient = new SearchServiceClient(Config.AZURE_SEARCH_SERVICE_NAME, new SearchCredentials(Config.AZURE_SEARCH_ADMIN_KEY));
            indexClient = serviceClient.Indexes.GetClient(Config.AZURE_SEARCH_INDEX_NAME);
            linkedEntityClient = new EntityLinkingServiceClient(Config.ENTITY_LINKING_API_KEY);
            cosmosDb = new AnnotationStore();

            // read the list of cia-cryptonymns
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EnricherFunction.cia-cryptonyms.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                cryptonymns = JsonConvert.DeserializeObject<Dictionary<string,string>>(reader.ReadToEnd());
            }
        }


        private static Task<EntityLink[]> DetectCIACryptonyms(string txt)
        {
            var words = txt.Split(' ', '\n', '\r');
            var ciaWords = words
                .Select(w => w.ToUpperInvariant())
                .Where(cryptonymns.ContainsKey)
                .Distinct()
                .Select(w => new EntityLink() {
                    Name = w
                });

            return Task.FromResult(ciaWords.ToArray());
        }

        private static Task<EntityLink[]> GetLinkedEntitiesAsync(params string[] txts)
        {
            var txt = string.Join(Environment.NewLine, txts);
            if (string.IsNullOrWhiteSpace(txt))
                return Task.FromResult<EntityLink[]>(null);

            // truncate each page to 10k charactors
            if (txt.Length > 10000)
                txt = txt.Substring(0, 10000);

            return linkedEntityClient.LinkAsync(txt);
        }

        private static async Task<AnnotatedPage> CombineMetadata(OcrResult ocr, OcrResult hw, AnalysisResult vis, EntityLink[] cia, EntityLink[] entities, ImageReference img)
        {
            // The handwriting result also included OCR text but OCR will produce better results on typed documents
            // so take the result that produces the most text.  Consider combining them by region to take the best of each.
            var result = hw.Text.Length > ocr.Text.Length ? ocr : hw;

            // create metadata for the vision caption and tags
            var captionLines = vis.Description.Captions.Select(caption => new lineResult()
            {
                words = caption
                    .Text
                    .Split(' ')
                    .Select(word => new WordResult() { text = word })
                    .ToArray()
            });

            var tagLines = new[] { new lineResult()
            {
                words = new[] { "(" }
                    .Concat(vis.Tags.Select(t => t.Name))
                    .Concat(new[] { ")" })
                    .Select(t => new WordResult() { text = t }).ToArray()
            }};

            var newResult = new OcrResult()
            {
                lines = result
                    .lines
                    .Concat(captionLines)
                    .Concat(tagLines).ToArray()
            };

            // rotate the image if needed
            var pageImg = ocr.Orientation == "Up" || ocr.Orientation == "NotDetected"
                ? img
                : await img.GetImage().Rotate(ocr.Orientation).UploadMedia(blobContainer);


            // TODO: merge annotations for Linked Entities and Cryptonyms
            return new AnnotatedPage(newResult, pageImg);
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
            var cryptonyms = skillSet.AddSkill("cia-cryptonyms",
                ocr => DetectCIACryptonyms(ocr.Text),
                cogOcr);

            // combine the data as an annotated page that can be used by the UI
            var pageContent = skillSet.AddSkill("page-metadata",
                CombineMetadata,
                cogOcr, handwriting, vision, cryptonyms, linkedEntities, resizedImage);

            return skillSet;
        }


        public static async Task Run(Stream blobStream, string name, TraceWriter log)
        {
            log.Info($"Processing blob:{name}");

            // parse the document to extract images
            IEnumerable<PageImage> pages = DocumentParser.Parse(blobStream).Pages;

            // create and apply the skill set to create annotations
            SkillSet<PageImage> skillSet = CreateCognitiveSkillSet();
            var annotations = await skillSet.ApplyAsync(pages);

            // Commit them to Cosmos DB to be used by full corpus skills such as Topics
            await cosmosDb.SaveAsync(annotations);

            // index the annotated document with azure search
            AnnotatedDocument document = new AnnotatedDocument(annotations.Select(a => a.Get<AnnotatedPage>("page-metadata")));
            var searchDocument = new SearchDocument(name)
            {
                Metadata = document.Metadata,
                Text = document.Text,
                LinkedEntities = annotations
                     .SelectMany(a => a.Get<EntityLink[]>("linked-entities") ?? new EntityLink[0])
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

