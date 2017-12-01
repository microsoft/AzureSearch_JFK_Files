using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace SearchUI.Controllers
{
    public class SearchController : ApiController
    {
        private DocSearch _docSearch = new DocSearch();

        [HttpPost]
        public JObject Post([FromBody]JObject body)
        {
            //var json = await Request.Content.ReadAsStringAsync();
            dynamic input = body as dynamic;
            var facetsIn = body.GetValue("facets") as JArray;
            var it = facetsIn.Where(t => t.Value<string>().StartsWith("terms")).FirstOrDefault();
            if (it != null)
                it.Remove();

            var result = _docSearch.Search(body.ToString());


            if (it != null)
            {
                dynamic dynResult = result as dynamic;

                string query = input.search;
                var facets = _docSearch.GetFacets(query, 10);

                foreach (var facet in facets.Facets)
                {
                    var facetList = result.GetValue("@search.facets") as JObject;
                    facetList.Add(facet.Key, JArray.FromObject(facet.Value));
                }
            }
            return result;
        }

    }
}