using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Cognitive.Skills
{
    public interface ISkill<T>
    {
        string Name { get; }

        T Get(Annotation input);

        Task<T> GetAsync(Annotation input);
    }

}
