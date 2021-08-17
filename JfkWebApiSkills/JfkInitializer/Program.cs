using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace JfkInitializer
{
    class Program
    {
        // Configurable names, these can be changed in the App.config file if you like
        private static string DataSourceName;
        private static string IndexName;
        private static string SkillsetName;
        private static string IndexerName;
        private static string SynonymMapName;
        private static string BlobContainerNameForImageStore;

        // Set this to true to see additional debugging information in the console.
        private static bool DebugMode = false;

        // Set this to true if you would like this app to deploy the JFK files frontend to your Azure site.
        private static bool ShouldDeployWebsite = true;

        // Clients
        private static SearchIndexClient _searchIndexClient;
        private static SearchIndexerClient _searchIndexerClient;
        private static HttpClient _httpClient = new HttpClient();

        static void Main(string[] args)
        {
            DataSourceName = ConfigurationManager.AppSettings["DataSourceName"];
            IndexName = ConfigurationManager.AppSettings["IndexName"];
            SkillsetName = ConfigurationManager.AppSettings["SkillsetName"];
            IndexerName = ConfigurationManager.AppSettings["IndexerName"];
            SynonymMapName = ConfigurationManager.AppSettings["SynonymMapName"];
            BlobContainerNameForImageStore = ConfigurationManager.AppSettings["BlobContainerNameForImageStore"];

            Uri searchServiceEndpoint = new Uri(string.Format("https://{0}.{1}", ConfigurationManager.AppSettings["SearchServiceName"], ConfigurationManager.AppSettings["SearchServiceDnsSuffix"]));
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            _searchIndexClient = new SearchIndexClient(searchServiceEndpoint, new AzureKeyCredential(apiKey));
            _searchIndexerClient = new SearchIndexerClient(searchServiceEndpoint, new AzureKeyCredential(apiKey));

            bool result = RunAsync().GetAwaiter().GetResult();
            if (!result && !DebugMode)
            {
                Console.WriteLine("Something went wrong.  Set 'DebugMode' to true in order to see traces.");
            }
            else if (!result)
            {
                Console.WriteLine("Something went wrong.");
            }
            else
            {
                Console.WriteLine("All operations were successful.");
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task<bool> RunAsync()
        {
            bool result = await DeleteIndexingResources();
            if (!result)
                return result;
            result = await CreateBlobContainerForImageStore();
            if (!result)
                return result;
            result = await CreateDataSource();
            if (!result)
                return result;
            result = await CreateSkillSet();
            if (!result)
                return result;
            result = await CreateSynonyms();
            if (!result)
                return result;
            result = await CreateIndex();
            if (!result)
                return result;
            result = await CreateIndexer();
            if (!result)
                return result;
            if (ShouldDeployWebsite)
            {
                result = await DeployWebsite();
            }
            result = await CheckIndexerStatus();
            if (!result)
                return result;
            result = await QueryIndex();
            return result;
        }

        private static async Task<bool> DeleteIndexingResources()
        {
            Console.WriteLine("Deleting Data Source, Index, Indexer, Skillset and SynonymMap if they exist...");
            try
            {
                await _searchIndexerClient.DeleteDataSourceConnectionAsync(DataSourceName);
                await _searchIndexClient.DeleteIndexAsync(IndexName);
                await _searchIndexerClient.DeleteIndexerAsync(IndexerName);
                await _searchIndexerClient.DeleteSkillsetAsync(SkillsetName);
                await _searchIndexClient.DeleteSynonymMapAsync(SynonymMapName);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error deleting resources: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CreateBlobContainerForImageStore()
        {
            Console.WriteLine("Creating Blob Container for Image Store Skill...");
            try
            {
                BlobContainerClient container = new BlobContainerClient(
                    connectionString: ConfigurationManager.AppSettings["BlobStorageAccountConnectionString"], 
                    blobContainerName: BlobContainerNameForImageStore);
                await container.CreateIfNotExistsAsync();
                // Note that setting this access policy means that the container will be publically accessible.  This is necessary for
                // the website to work properly.  Remove this next line if you start using this code to process any private or
                // confidential data, but note that the website will stop working properly if you do.
                await container.SetAccessPolicyAsync(PublicAccessType.BlobContainer);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating blob container: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CreateDataSource()
        {
            Console.WriteLine("Creating Data Source...");
            try
            {
                SearchIndexerDataSourceConnection dataSource = SearchResources.GetDataSource(DataSourceName);
                await _searchIndexerClient.CreateDataSourceConnectionAsync(dataSource);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating data source: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CreateSkillSet()
        {
            Console.WriteLine("Creating Skill Set...");
            try
            {
                SearchIndexerSkillset skillset = SearchResources.GetSkillset(SkillsetName, BlobContainerNameForImageStore);
                await _searchIndexerClient.CreateSkillsetAsync(skillset);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating skillset: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CreateSynonyms()
        {
            Console.WriteLine("Creating Synonym Map...");
            try
            {
                SynonymMap synonyms = SearchResources.GetSynonymMap(SynonymMapName);
                await _searchIndexClient.CreateSynonymMapAsync(synonyms);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating synonym map: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CreateIndex()
        {
            Console.WriteLine("Creating Index...");
            try
            {
                SearchIndex index = SearchResources.GetIndex(IndexName, SynonymMapName);
                await _searchIndexClient.CreateIndexAsync(index);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating index: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CreateIndexer()
        {
            Console.WriteLine("Creating Indexer...");
            try
            {
                SearchIndexer indexer = SearchResources.GetIndexer(IndexerName, DataSourceName, IndexName, SkillsetName);
                await _searchIndexerClient.CreateIndexerAsync(indexer);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error creating indexer: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> DeployWebsite()
        {
            try
            {
                Console.WriteLine("Setting Website Keys...");
                string searchQueryKey = ConfigurationManager.AppSettings["SearchServiceQueryKey"];
                string envText = File.ReadAllText("../../../../frontend/.env");
                envText = envText.Replace("[SearchServiceName]", ConfigurationManager.AppSettings["SearchServiceName"]);
                envText = envText.Replace("[SearchServiceDomain]", ConfigurationManager.AppSettings["SearchServiceDnsSuffix"]);
                envText = envText.Replace("[IndexName]", IndexName);
                envText = envText.Replace("[SearchServiceApiKey]", searchQueryKey);
                envText = envText.Replace("[SearchServiceApiVersion]", ConfigurationManager.AppSettings["SearchServiceApiVersion"]);
                envText = envText.Replace("[AzureFunctionName]", ConfigurationManager.AppSettings["AzureFunctionSiteName"]);
                envText = envText.Replace("[AzureFunctionDefaultHostKey]", ConfigurationManager.AppSettings["AzureFunctionHostKey"]);
                File.WriteAllText("../../../../frontend/.env", envText);

                Console.WriteLine("Website keys have been set.  Please build the website and then return here and press any key to continue.");
                Console.ReadKey();

                Console.WriteLine("Deploying Website...");
                if (File.Exists("website.zip"))
                {
                    File.Delete("website.zip");
                }
                ZipFile.CreateFromDirectory("../../../../frontend/dist", "website.zip");
                byte[] websiteZip = File.ReadAllBytes("website.zip");
                HttpContent content = new ByteArrayContent(websiteZip);
                string uri = String.Format("https://{0}.scm.azurewebsites.net/api/zipdeploy?isAsync=true", ConfigurationManager.AppSettings["AzureWebAppSiteName"]);

                byte[] credentials = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", ConfigurationManager.AppSettings["AzureWebAppUsername"], ConfigurationManager.AppSettings["AzureWebAppPassword"]));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

                HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
                if (DebugMode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Deploy website response: \n{0}", responseText);
                }
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }
                Console.WriteLine("Website deployment accepted.  Waiting for deployment to complete...");
                IEnumerable<string> values;
                if (response.Headers.TryGetValues("Location", out values))
                {
                    string pollingUri = values.First();
                    bool complete = false;
                    while (!complete)
                    {
                        Thread.Sleep(3000);
                        HttpResponseMessage pollingResponse = await _httpClient.GetAsync(pollingUri);
                        string responseText = await pollingResponse.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(responseText);
                        complete = json.SelectToken("complete") == null ? false : json.SelectToken("complete").ToObject<bool>();
                        if (DebugMode)
                        {
                            Console.WriteLine("Current website deployment status: {0}", json.SelectToken("progress")?.ToString());
                        }
                    }
                    Console.WriteLine("Website deployment completed.");
                }
                else
                {
                    Console.WriteLine("Could not find polling url from response.");
                }
                Console.WriteLine("Website url: https://{0}.azurewebsites.net/", ConfigurationManager.AppSettings["AzureWebAppSiteName"]);
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error deploying website: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }

        private static async Task<bool> CheckIndexerStatus()
        {
            Console.WriteLine("Waiting for indexing to complete...");
            IndexerExecutionStatus requestStatus = IndexerExecutionStatus.InProgress;
            try
            {
                await _searchIndexerClient.GetIndexerAsync(IndexerName);
                while (requestStatus.Equals(IndexerExecutionStatus.InProgress))
                {
                    Thread.Sleep(3000);
                    SearchIndexerStatus info = await _searchIndexerClient.GetIndexerStatusAsync(IndexerName);
                    requestStatus = info.LastResult.Status;
                    if (DebugMode)
                    {
                        Console.WriteLine("Current indexer status: {0}", requestStatus.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error retrieving indexer status: {0}", ex.Message);
                }
                return false;
            }
            return requestStatus.Equals(IndexerExecutionStatus.Success);
        }

        private static async Task<bool> QueryIndex()
        {
            Console.WriteLine("Querying Index...");
            try
            {
                SearchClient indexClient = _searchIndexClient.GetSearchClient(IndexName);
                SearchResults<object> searchResult = await indexClient.SearchAsync<object>("*");
                Console.WriteLine("Number of Query Results: {0}", searchResult.GetResults().Count());
            }
            catch (Exception ex)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Error querying index: {0}", ex.Message);
                }
                return false;
            }
            return true;
        }
    }
}
