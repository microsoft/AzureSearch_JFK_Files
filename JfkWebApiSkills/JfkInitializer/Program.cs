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
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

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
        private static ISearchServiceClient _searchClient;
        private static HttpClient _httpClient = new HttpClient();

        static void Main(string[] args)
        {
            DataSourceName = ConfigurationManager.AppSettings["DataSourceName"];
            IndexName = ConfigurationManager.AppSettings["IndexName"];
            SkillsetName = ConfigurationManager.AppSettings["SkillsetName"];
            IndexerName = ConfigurationManager.AppSettings["IndexerName"];
            SynonymMapName = ConfigurationManager.AppSettings["SynonymMapName"];
            BlobContainerNameForImageStore = ConfigurationManager.AppSettings["BlobContainerNameForImageStore"];

            string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));

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
                await _searchClient.DataSources.DeleteAsync(DataSourceName);
                await _searchClient.Indexes.DeleteAsync(IndexName);
                await _searchClient.Indexers.DeleteAsync(IndexerName);
                await _searchClient.Skillsets.DeleteAsync(SkillsetName);
                await _searchClient.SynonymMaps.DeleteAsync(SynonymMapName);
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
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["BlobStorageAccountConnectionString"]);
                CloudBlobClient client = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(BlobContainerNameForImageStore);
                await container.CreateIfNotExistsAsync();
                // Note that setting this permission means that the container will be publically accessible.  This is necessary for
                // the website to work properly.  Remove these next 3 lines if you start using this code to process any private or
                // confidential data, but note that the website will stop working properly if you do.
                BlobContainerPermissions permissions = container.GetPermissions();
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                await container.SetPermissionsAsync(permissions);
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
                DataSource dataSource = SearchResources.GetDataSource(DataSourceName);
                await _searchClient.DataSources.CreateAsync(dataSource);
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
                Skillset skillset = SearchResources.GetSkillset(SkillsetName, await KeyHelper.GetAzureFunctionHostKey(_httpClient), BlobContainerNameForImageStore);
                await _searchClient.Skillsets.CreateAsync(skillset);
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
                await _searchClient.SynonymMaps.CreateAsync(synonyms);
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
                Index index = SearchResources.GetIndex(IndexName, SynonymMapName);
                await _searchClient.Indexes.CreateAsync(index);
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
                Indexer indexer = SearchResources.GetIndexer(IndexerName, DataSourceName, IndexName, SkillsetName);
                await _searchClient.Indexers.CreateAsync(indexer);
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
                envText = envText.Replace("[SearchServiceDomain]", _searchClient.SearchDnsSuffix);
                envText = envText.Replace("[IndexName]", IndexName);
                envText = envText.Replace("[SearchServiceApiKey]", searchQueryKey);
                envText = envText.Replace("[SearchServiceApiVersion]", _searchClient.ApiVersion);
                envText = envText.Replace("[AzureFunctionName]", ConfigurationManager.AppSettings["AzureFunctionSiteName"]);
                envText = envText.Replace("[AzureFunctionDefaultHostKey]", await KeyHelper.GetAzureFunctionHostKey(_httpClient));
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
                await _searchClient.Indexers.GetAsync(IndexerName);
                while (requestStatus.Equals(IndexerExecutionStatus.InProgress))
                {
                    Thread.Sleep(3000);
                    IndexerExecutionInfo info = await _searchClient.Indexers.GetStatusAsync(IndexerName);
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
                ISearchIndexClient indexClient = _searchClient.Indexes.GetClient(IndexName);
                DocumentSearchResult<Document> searchResult = await indexClient.Documents.SearchAsync("*");
                Console.WriteLine("Query Results:");
                foreach (SearchResult<Document> result in searchResult.Results)
                {
                    foreach (string key in result.Document.Keys)
                    {
                        Console.WriteLine("{0}: {1}", key, result.Document[key]);
                    }
                }
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
