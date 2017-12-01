using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Cognitive.Skills
{
    /// <summary>
    /// Indexed representation of a scanned document that uses the HOCR standard for encoding word position metadata of OCR documents 
    /// </summary>
    [SerializePropertyNamesAsCamelCase]
    public class SearchDocument
    {
        public SearchDocument(string name)
        {
            this.Id = name;
        }

        public SearchDocument()
        {
        }


        // Fields that are in the index
        private string id;

        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string Id { get { return id; } set { id = value.Replace(".", "_").Replace(" ", "_"); } }


        [IsSearchable]
        public string Metadata
        {
            get;
            set;
        }

        [IsRetrievable(false)]
        [IsSearchable]
        public string Text
        {
            get;
            set;
        }
        
        [IsFilterable]
        [IsFacetable]
        [JsonProperty("entities")]
        public List<string> LinkedEntities { get; set; } = new List<string>();
        
    }
}
