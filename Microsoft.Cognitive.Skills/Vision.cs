using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Cognitive.Skills
{
    public class Vision
    {
        VisionServiceClient visionClient;
        private string apiKey;
        private string apiRoot;

        public Vision(string visionApiKey, string visionRegion)
        {
            apiKey = visionApiKey;
            apiRoot = $"https://{visionRegion}/vision/v1.0";
            visionClient = new VisionServiceClient(visionApiKey, apiRoot);
        }


        private static int[] ConvertBoundingBox(string bbText)
        {
            var bbox = bbText.Split(',').Select(b => int.Parse(b)).ToArray();
            //0-left
            //1-top
            //2-width
            //3-height
            return new int[] {
                bbox[0],         bbox[1],
                bbox[0]+bbox[2], bbox[1],
                bbox[0]+bbox[2], bbox[1] + bbox[3],
                bbox[0],         bbox[1] + bbox[3],
            };
        }

        public Task<OcrResult> RecognizeTextAsync(string url)
        {
            return GetText(null, url, null);
        }

        public Task<OcrResult> GetText(string url, string name)
        {
            return GetText(null, url, name);
        }

        public async Task<OcrResult> GetText(Stream stream, string url = null, string name = null)
        {
            var visionResult = await (stream != null ? visionClient.RecognizeTextAsync(stream, "en", true) : visionClient.RecognizeTextAsync(url, "en", true));

            var lines = visionResult.Regions.SelectMany(r => r.Lines).Select(l =>

                new lineResult()
                {
                    boundingBox = ConvertBoundingBox(l.BoundingBox),
                    words = l.Words.Select(w => new WordResult()
                    {
                        boundingBox = ConvertBoundingBox(w.BoundingBox),
                        text = w.Text
                    }).ToArray()
                }
            );

            
            var result = new OcrResult()
            {
                lines = lines.ToArray(),
                Orientation = visionResult.Orientation
            };
            return result;
        }

        public Task<OcrResult> GetVision(string url)
        {
            return GetVision(null, url);
        }


        public Task<AnalysisResult> AnalyzeImageAsync(string url, VisualFeature[] features)
        {
            return visionClient.AnalyzeImageAsync(url, features);
        }

        public async Task<OcrResult> GetVision(Stream stream, string url = null)
        {
            var features = new[] { VisualFeature.Tags, VisualFeature.ImageType, VisualFeature.Description, VisualFeature.Adult};
            var visionResult = stream != null ? 
                await visionClient.AnalyzeImageAsync(stream, features)
                : await visionClient.AnalyzeImageAsync(url, features);

            List<lineResult> lines = new List<lineResult>();
            lines.AddRange(visionResult.Description.Captions.Select(c => new lineResult()
            {
                words = c.Text.Split(' ').Select(w => new WordResult()
                {
                    text = w
                }).ToArray()
            }
            ));

            lines.Add(new lineResult()
            {
                words = new[] { "(" }
                    .Concat(visionResult.Tags.Select(t => t.Name))
                    .Concat(new[] { ")" })
                    .Select(t => new WordResult() { text = t }).ToArray()
            });

            var result = new OcrResult()
            {
                lines = lines.ToArray()
            };
            return result;
        }

        public Task<OcrResult> GetHandwritingTextAsync(string imageUrl)
        {
            return GetHandwritingText(imageUrl, null);
        }

        public Task<OcrResult> GetHandwritingText(string imageUrl, string name)
        {
            return GetHandwritingTextImpl(null, imageUrl);
        }

        public Task<OcrResult> GetHandwritingText(Stream stream, string name)
        {
            return GetHandwritingTextImpl(stream, null);
        }

        private async Task<OcrResult> GetHandwritingTextImpl(Stream stream, string url)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            var uri = apiRoot + "/recognizeText?handwriting=true";

            HttpResponseMessage response;

            // Request body
            if (stream != null)
            {
                using (var content = new StreamContent(stream))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(uri, content);
                }
            }
            else
            {
                var json = JsonConvert.SerializeObject(new { url = url });
                using (var content = new StringContent(json))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                }
            }

            OcrResult result = null;
            IEnumerable<string> opLocation;

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
            }



            if (response.Headers.TryGetValues("Operation-Location", out opLocation))
            {
                while (true)
                {
                    response = await client.GetAsync(opLocation.First());
                    var txt = await response.Content.ReadAsStringAsync();
                    var status = JsonConvert.DeserializeObject<AsyncStatusResult>(txt);
                    if (status.status == "Running" || status.status == "NotStarted")
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    else
                    {
                        result = status.recognitionResult;

                        break;
                    }
                }
            }

            return result;
        }
    }


    public class AsyncStatusResult
    {
        public string status { get; set; }
        public OcrResult recognitionResult { get; set; }
    }


    // result for Ocr used by Handwriting
    public class OcrResult
    {
        public string Orientation { get; set; } = "NotDetected";
        public lineResult[] lines { get; set; }

        public string Text {
            get
            {
                return string.Join(" ", lines.SelectMany(l => l.words).Select(w => w.text));
            }
        }
    }

    public class RegionResult
    {
        internal const int XUL = 0;
        internal const int YUL = 1;
        internal const int XUR = 2;
        internal const int YUR = 3;
        internal const int XLR = 4;
        internal const int YLR = 5;
        internal const int XLL = 6;
        internal const int YLL = 7;

        public int[] boundingBox { get; set; }
        public string text { get; set; }


        public IEnumerable<Point> GetPoints()
        {
            for (int i = 0; i < boundingBox.Length; i += 2)
            {
                yield return new Point(boundingBox[i], boundingBox[i + 1]);
            }
            yield return new Point(boundingBox[0], boundingBox[1]);
        }

        public int CenterY { get { return StartY + (Height / 2); } }
        public int StartX { get { return boundingBox[XUL]; } }
        public int StartY { get { return boundingBox[YUL]; } }
        public int Height { get { return boundingBox[YLL] - boundingBox[YUL]; } }
        public int Width { get { return boundingBox[XUR] - boundingBox[XUL]; } }
    }

    public class lineResult : RegionResult
    {
        public WordResult[] words { get; set; }
    }


    public class WordResult : RegionResult
    {
    }
}
