using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace Microsoft.Cognitive.Skills
{
    public class AnnotationStore
    {
        private DocumentClient docClient;

        public AnnotationStore(string serviceName, string key)
        {
            docClient = new DocumentClient(new Uri($"https://{serviceName}.documents.azure.com:443/"), key);
        }

        public Task SaveAsync(AnnotatedDocument doc)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(IEnumerable<Annotation> annotations)
        {
            throw new NotImplementedException();
        }

        public Task<Annotation> CreateOrGetAsync(string id)
        {
            throw new NotImplementedException();
        }
    }



    public class Annotation
    {
        private Dictionary<string, object> fields = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, IEnumerable<Annotation>> linkedAnnotations = new Dictionary<string, IEnumerable<Annotation>>();

        // row key
        public string Id { get { return (string)fields["id"]; } set { fields["id"] = value; } }

        // parition id
        public string ParentId { get { return (string)fields["parentId"]; } set { fields["parentId"] = value; } }

        public string ItemType { get { return (string)fields["annotationType"]; } set { fields["itemType"] = value; } }

        public T Get<T>(ISkill<T> enrichment)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string name)
        {
            throw new NotImplementedException();
        }
    }
}
