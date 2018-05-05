using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CognitiveSearch.Skills.Redaction
{
    public class RedactionClassifier
    {
        HttpClient client = new HttpClient();

        public RedactionClassifier() : this(Config.REDACTION_ENDPOINT) { }

        public RedactionClassifier(string url)
        {
            client.BaseAddress = new Uri(url);
        }

        public async Task<double> ClassifyImage(string base64ImageData)
        {
            string body = "[{\"image_in_base64\":\"b'" + base64ImageData + "'\",\"parameters\":{\"classification-add_softmax\":true}}]";
            HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage result = await client.PostAsync("", content);
            result.EnsureSuccessStatusCode();
            dynamic data = await result.Content.ReadAsAsync<dynamic>();

            JArray s = data as JArray;
            double score = double.Parse(((JValue)s[0]).Value.ToString().TrimStart('[').TrimEnd(']').Split(',')[1]);
            double prob = score * 100f;

            return prob;
        }
    }
}
