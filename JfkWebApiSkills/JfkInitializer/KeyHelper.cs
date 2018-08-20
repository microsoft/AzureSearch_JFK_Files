using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace JfkInitializer
{
    class KeyHelper
    {
        public static async Task<string> GetAzureFunctionHostKeyAsync(IConfiguration configuration, HttpClient client)
        {
            string uri = String.Format("https://{0}.scm.azurewebsites.net/api/functions/admin/masterkey", configuration["azureFunctionSiteName"]);

            byte[] credentials = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", configuration["azureFunctionUsername"], configuration["azureFunctionPassword"]));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            HttpResponseMessage response = await client.GetAsync(uri);
            string responseText = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseText);
            return json.SelectToken("masterKey").ToString();
        }
    }
}
