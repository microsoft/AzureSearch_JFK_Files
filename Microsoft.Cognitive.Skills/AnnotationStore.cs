using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Cognitive.Skills
{
    public class AnnotationStore
    {

        public AnnotationStore()
        {
        }

        public Task SaveAsync(AnnotatedDocument doc)
        {
            // TODO: This implementation will come later
            return Task.CompletedTask;
        }

        public Task SaveAsync(IEnumerable<Annotation> annotations)
        {
            // TODO: This implementation will come later
            return Task.CompletedTask;
        }

    }



    public class Annotation
    {
        private Dictionary<string, object> fields = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        public T Get<T>(ISkill<T> enrichment)
        {
            return Get<T>(enrichment.Name);
        }

        public T Get<T>(string name)
        {
            T value;
            TryGet(name, out value);
            return value;
        }

        public bool TryGet<T>(string name, out T value)
        {
            object objValue;
            bool result = fields.TryGetValue(name, out objValue);
            value = result ? (T)objValue : default(T);
            return result;
        }

        public void Set(string name, object value)
        {
            fields[name] = value;
        }

    }
}
