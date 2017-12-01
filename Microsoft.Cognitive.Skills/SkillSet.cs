using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.Cognitive.Skills
{

    public class SkillSet<TInput>
    {
        public static SkillSet<TInput> Create(string type, Func<TInput, string> idSelector)
        {
            throw new NotImplementedException();
        }

        public ISkill<TInput> Input
        {
            get { throw new NotImplementedException(); }
        }


        public Task<AnnotatedDocument> ApplyAsync(IEnumerable<TInput> input)
        {
            throw new NotImplementedException();
        }


        #region AddSkill overloads

        // Synchronous enrichments
        public ISkill<TResult> AddSkill<TInput1, TResult>(string name, Func<TInput1, TResult> action, ISkill<TInput1> input1)
        {
            throw new NotImplementedException();
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TResult>(string name, Func<TInput1, TInput2, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2)
        {
            throw new NotImplementedException();
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TResult>(string name, Func<TInput1, TInput2, TInput3, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3)
        {
            throw new NotImplementedException();
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4)
        {
            throw new NotImplementedException();
        }

        // Asynchronous enrichments
        public ISkill<TResult> AddSkill<TInput1, TResult>(string name, Func<TInput1, Task<TResult>> action, ISkill<TInput1> input1)
        {
            throw new NotImplementedException();
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TResult>(string name, Func<TInput1, TInput2, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2)
        {
            throw new NotImplementedException();
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TResult>(string name, Func<TInput1, TInput2, TInput3, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3)
        {
            throw new NotImplementedException();
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4) where TResult : new()
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
