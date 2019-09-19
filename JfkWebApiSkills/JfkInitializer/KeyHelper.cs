using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JfkInitializer
{
    static class KeyHelper
    {
        private static string _azureFunctionHostKey;

        public static async Task<string> GetAzureFunctionHostKey(HttpClient client)
        {
            if (_azureFunctionHostKey == null)
            {
                string uri = String.Format("https://{0}.scm.azurewebsites.net/api/functions/admin/masterkey", ConfigurationManager.AppSettings["AzureFunctionSiteName"]);

                byte[] credentials = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", ConfigurationManager.AppSettings["AzureFunctionUsername"], ConfigurationManager.AppSettings["AzureFunctionPassword"]));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

                HttpResponseMessage response = await client.GetAsync(uri);
                string responseText = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseText);
                _azureFunctionHostKey = json.SelectToken("masterKey").ToString();
            }
            return _azureFunctionHostKey;
        }
    }
}
