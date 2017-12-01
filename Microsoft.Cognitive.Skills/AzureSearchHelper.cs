using Microsoft.Azure.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Text;

namespace Microsoft.Cognitive.Skills
{
    public class AzureSearchHelper
    {
        private const string DefaultApiVersionString = "2016-09-01";
        private static Uri _serviceUri;
        private static HttpClient _httpClient;

        public AzureSearchHelper(string serviceName, string apiKey)
        {
            _serviceUri = new Uri("https://" + serviceName + ".search.windows.net");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
        }

        public string Post(string uriPath, string json, string version = DefaultApiVersionString)
        {
            return SendRequest(uriPath, json, HttpMethod.Post, version);
        }

        public string Put(string uriPath, string json, string version = DefaultApiVersionString)
        {
            return SendRequest(uriPath, json, HttpMethod.Put, version);
        }

        public string Get(string uriPath, string version = DefaultApiVersionString)
        {
            return SendRequest(uriPath, null, HttpMethod.Get, version);
        }

        public string SendRequest(string uriPath, string json, HttpMethod method, string version = DefaultApiVersionString)
        {
            Uri uri = new Uri(_serviceUri, uriPath);
            UriBuilder builder = new UriBuilder(uri);
            string separator = string.IsNullOrWhiteSpace(builder.Query) ? string.Empty : "&";
            builder.Query = builder.Query.TrimStart('?') + separator + "api-version=" + version;

            var request = new HttpRequestMessage(method, builder.Uri);

            if (json != null)
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = _httpClient.SendAsync(request).Result;

            EnsureSuccessfulSearchResponse(response);

            return response.Content.ReadAsStringAsync().Result;
        }

        private void EnsureSuccessfulSearchResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string error = response.Content == null ? null : response.Content.ReadAsStringAsync().Result;
                throw new Exception("Search request failed: " + error);
            }
        }
    }
}