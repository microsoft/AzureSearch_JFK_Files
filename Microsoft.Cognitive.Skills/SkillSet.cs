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
        private string name;
        private List<ISkillProcessor> skills = new List<ISkillProcessor>();

        public static SkillSet<TInput> Create(string name, Func<TInput, string> idSelector)
        {
            return new SkillSet<TInput>() { name = name };
        }

        public ISkill<TInput> Input
        {
            get { return new Skill<TInput>(name, data => Task.FromResult(default(TInput))); }
        }


        public async Task<IEnumerable<Annotation>> ApplyAsync(IEnumerable<TInput> input)
        {
            List<Annotation> annotations = new List<Annotation>();
            foreach (var data in input)
            {
                var annotation = new Annotation();
                annotation.Set(name, data);
                annotations.Add(annotation);

                foreach (var skill in skills)
                {
                    await skill.Process(annotation);
                }
            }

            return annotations;
        }


        #region AddSkill overloads

        // Synchronous enrichments
        public ISkill<TResult> AddSkill<TInput1, TResult>(string name, Func<TInput1, TResult> action, ISkill<TInput1> input1)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                action(await input1.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TResult>(string name, Func<TInput1, TInput2, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                action(await input1.GetAsync(annotation), await input2.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TResult>(string name, Func<TInput1, TInput2, TInput3, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation), await input4.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TInput5, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, TInput5, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4, ISkill<TInput5> input5)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation), await input4.GetAsync(annotation), await input5.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TResult> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4, ISkill<TInput5> input5, ISkill<TInput6> input6)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation), await input4.GetAsync(annotation), await input5.GetAsync(annotation), await input6.GetAsync(annotation))
            ));
        }

        // Asynchronous enrichments
        public ISkill<TResult> AddSkill<TInput1, TResult>(string name, Func<TInput1, Task<TResult>> action, ISkill<TInput1> input1)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                await action(await input1.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TResult>(string name, Func<TInput1, TInput2, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                await action(await input1.GetAsync(annotation), await input2.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TResult>(string name, Func<TInput1, TInput2, TInput3, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                await action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                await action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation), await input4.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TInput5, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, TInput5, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4, ISkill<TInput5> input5)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                await action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation), await input4.GetAsync(annotation), await input5.GetAsync(annotation))
            ));
        }

        public ISkill<TResult> AddSkill<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TResult>(string name, Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, Task<TResult>> action, ISkill<TInput1> input1, ISkill<TInput2> input2, ISkill<TInput3> input3, ISkill<TInput4> input4, ISkill<TInput5> input5, ISkill<TInput6> input6)
        {
            return AddSkillProcessor(new Skill<TResult>(name, async annotation =>
                await action(await input1.GetAsync(annotation), await input2.GetAsync(annotation), await input3.GetAsync(annotation), await input4.GetAsync(annotation), await input5.GetAsync(annotation), await input6.GetAsync(annotation))
            ));
        }
        #endregion


        private ISkill<T> AddSkillProcessor<T>(ISkill<T> skill)
        {
            skills.Add(skill);
            return skill;
        }
    }
}
