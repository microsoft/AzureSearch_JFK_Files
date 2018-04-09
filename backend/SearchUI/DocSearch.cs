using EnricherFunction;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Cognitive.Skills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace SearchUI
{
    public class DocSearch
    {
        private static AzureSearchHelper searchHelper;
        private static SearchServiceClient _searchClient;

        private string indexName;
        public static string errorMessage;

        public DocSearch()
        {
            try
            {
                searchHelper = new AzureSearchHelper(Config.AZURE_SEARCH_SERVICE_NAME, Config.AZURE_SEARCH_ADMIN_KEY);
                indexName = Config.AZURE_SEARCH_INDEX_NAME;

                // Create an HTTP reference to the catalog index
                _searchClient = new SearchServiceClient(Config.AZURE_SEARCH_SERVICE_NAME, new SearchCredentials(Config.AZURE_SEARCH_ADMIN_KEY));
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }
        

        public DocumentSearchResult GetFacets(string searchText, int maxCount = 30)
        {
            // Execute search based on query string
            try
            {
                SearchParameters sp = new SearchParameters()
                {
                    SearchMode = SearchMode.Any,
                    Top = 0,
                    Select = new List<String>() { "id" },
                    Facets = new List<String>() { "terms, count:" + maxCount },
                    QueryType = QueryType.Full
                };

                return _searchClient.Indexes.GetClient(indexName).Documents.Search(searchText, sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public JObject Search(string json)
        {
            var response = searchHelper.Post("/indexes/" + indexName + "/docs/search", json);
            return JObject.Parse(response);
        }
    }
}