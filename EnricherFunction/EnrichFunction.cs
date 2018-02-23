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
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

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
            visionClient = new Vision(Config.VISION_API_KEY, Config.VISION_API_REGION);
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


#region Azure Function Entry points

        [FunctionName("index-document")]
        public static async Task<HttpResponseMessage> HttpProcessDocument([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // parse query parameter
            string name = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0).Value;
            if (string.IsNullOrEmpty(name))
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body");

            // Get request body
            var stream = await req.Content.ReadAsStreamAsync();

            try
            {
                await Run(stream, name, log);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return req.CreateResponse(HttpStatusCode.InternalServerError, "Error processing the Document: " + e.ToString());
            }

            return req.CreateResponse(HttpStatusCode.OK, $"Document {name} was added to the index");
        }


        [FunctionName("get-annotated-document")]
        public static async Task<HttpResponseMessage> HttpGetAnnotatedDocument([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // parse query parameter
            string name = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0).Value;
            if (string.IsNullOrEmpty(name))
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body");

            // Get request body
            var stream = await req.Content.ReadAsStreamAsync();

            try
            {
                log.Info($"Annotating Document:{name}");
                var annotations = await ProcessDocument(stream);
                return req.CreateResponse(HttpStatusCode.OK, annotations.ToArray());
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return req.CreateResponse(HttpStatusCode.InternalServerError, "Error processing the Document: " + e.ToString());
            }
        }

        [FunctionName("get-search-document")]
        public static async Task<HttpResponseMessage> HttpGetSearchDocument([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // parse query parameter
            string name = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0).Value;
            if (string.IsNullOrEmpty(name))
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body");

            // Get request body
            var stream = await req.Content.ReadAsStreamAsync();

            try
            {
                log.Info($"Creating Search Document:{name}");
                var annotations = await ProcessDocument(stream);
                var searchDocument = CreateSearchDocument(name, annotations);

                return req.CreateResponse(HttpStatusCode.OK, searchDocument);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return req.CreateResponse(HttpStatusCode.InternalServerError, "Error processing the Document: " + e.ToString());
            }
        }

        [FunctionName("index-document-blob-trigger")]
        public static async Task BlobTriggerIndexDocument([BlobTrigger(Config.LIBRARY_BLOB_STORAGE_CONTAINER + "/{name}", Connection = "IMAGE_BLOB_CONNECTION_STRING")]Stream blobStream, string name, TraceWriter log)
        {
            await Run(blobStream, name, log);
        }


        [FunctionName("web-api-skill")]
        public static async Task<HttpResponseMessage> WebApiSkill([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            try
            {
                // Get request body
                var jsonRequest = await req.Content.ReadAsStringAsync();
                var docs = JsonConvert.DeserializeObject<WebApiSkillRequest>(jsonRequest);

                WebApiSkillResponse response = new WebApiSkillResponse();

                HttpClient httpClient = new HttpClient();

                foreach (var inRecord in docs.values)
                {
                    var outRecord = new WebApiResponseRecord() { recordId = inRecord.recordId };
                    
                    string name = inRecord.data["name"] as string;
                    log.Info($"Creating Search Document:{name}");
                    string blobUrl = ((string)inRecord.data["url"]) + inRecord.data["querystring"] as string;

                    try
                    {
                        log.Info($"Downloading Document:{blobUrl}");
                        var aa = await httpClient.GetAsync(blobUrl);
                        aa.EnsureSuccessStatusCode();
                        using (var stream = await aa.Content.ReadAsStreamAsync())
                        {
                            log.Info($"Processing Document...");
                            var annotations = await ProcessDocument(stream);

                            log.Info($"Creating Search Document...");
                            var searchDocument = CreateSearchDocument(name, annotations);
                            log.Info($"Document complete");

                            outRecord.data["metadata"] = searchDocument.Metadata;
                            outRecord.data["text"] = searchDocument.Text;
                            outRecord.data["entities"] = searchDocument.LinkedEntities;
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(e.ToString());
                        outRecord.errors.Add("Error processing the Document: " + e.ToString());
                    }
                    response.values.Add(outRecord);
                }

                return req.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return req.CreateResponse(HttpStatusCode.BadRequest, "Error: " + ex.ToString());
            }
        }

        public class WebApiSkillRequest
        {
            public List<WebApiRequestRecord> values { get; set; } = new List<WebApiRequestRecord>();
        }

        public class WebApiSkillResponse
        {
            public List<WebApiResponseRecord> values { get; set; } = new List<WebApiResponseRecord>();
        }

        public class WebApiRequestRecord
        {
            public string recordId { get; set; }
            public Dictionary<string, object> data { get; set; } = new Dictionary<string, object>();
        }

        public class WebApiResponseRecord
        {
            public string recordId { get; set; }
            public Dictionary<string, object> data { get; set; } = new Dictionary<string, object>();
            public List<string> errors { get; set; } = new List<string>();
            public List<string> warnings { get; set; } = new List<string>();
        }

        #endregion

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

            // Process the document and extract annotations
            IEnumerable<Annotation> annotations = await ProcessDocument(blobStream);

            // Commit them to Cosmos DB to be used by full corpus skills such as Topics
            await cosmosDb.SaveAsync(annotations);

            // Create Search Document and add it to the index
            SearchDocument searchDocument = CreateSearchDocument(name, annotations);
            await AddToIndex(name, searchDocument, log);
        }

        private static async Task<IEnumerable<Annotation>> ProcessDocument(Stream blobStream)
        {
            // parse the document to extract images
            IEnumerable<PageImage> pages = DocumentParser.Parse(blobStream).Pages;

            // create and apply the skill set to create annotations
            SkillSet<PageImage> skillSet = CreateCognitiveSkillSet();
            var annotations = await skillSet.ApplyAsync(pages);
            return annotations;
        }

        private static async Task AddToIndex(string name, SearchDocument searchDocument, TraceWriter log)
        {
            var batch = IndexBatch.MergeOrUpload(new[] { searchDocument });
            var result = await indexClient.Documents.IndexAsync(batch);

            if (!result.Results[0].Succeeded)
                log.Error($"index failed for {name}: {result.Results[0].ErrorMessage}");
        }

        private static SearchDocument CreateSearchDocument(string name, IEnumerable<Annotation> annotations)
        {
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
                     .Where(l => !string.IsNullOrEmpty(l))
                     .ToList(),
            };
            return searchDocument;
        }
    }
}

