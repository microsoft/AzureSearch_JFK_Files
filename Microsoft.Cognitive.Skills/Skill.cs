using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Cognitive.Skills
{
    public interface ISkillProcessor
    {
        Task Process(Annotation input);
    }

    public interface ISkill<T> : ISkillProcessor
    {
        string Name { get; }

        Task<T> GetAsync(Annotation input);
    }

    public class Skill<T> : ISkill<T>
    {
        public string Name { get; private set; }

        private Func<Annotation, Task<T>> transformAsync;

        public Skill(string name, Func<Annotation, Task<T>> transformAsync)
        {
            Name = name;
            this.transformAsync = transformAsync;
        }

        public async Task<T> GetAsync(Annotation input)
        {
            T value;
            if (!input.TryGet<T>(Name, out value))
            {
                Console.WriteLine("Processing " + Name);
                value = await transformAsync(input);
                input.Set(Name, value);
            }

            return value;
        }

        public Task Process(Annotation input) => GetAsync(input);
    }


}
