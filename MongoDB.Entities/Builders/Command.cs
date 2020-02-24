using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace MongoDB.Entities
{
    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    public class Command
    {
        private static readonly Regex regex = new Regex("<(.*)>", RegexOptions.Compiled);
        private readonly StringBuilder builder;
        private readonly HashSet<string> tags, missingTags, unReplacedTags;

        /// <summary>
        /// Initialize a command builder with the supplied template stirng.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Command(string template)
        {
            builder = new StringBuilder(template, template.Length);
            tags = new HashSet<string>();
            missingTags = new HashSet<string>();
            unReplacedTags = new HashSet<string>();

            foreach (Match match in regex.Matches(template))
            {
                tags.Add(match.Groups[0].Value);
            }

            if (tags.Count == 0)
                throw new ArgumentException("No replacement tags marked with <tagname> were found in the supplied template string");
        }

        private Command Replace(string path)
        {
            var tag = $"<{path}>";

            if (!tags.Contains(tag))
                missingTags.Add(tag);

            builder.Replace(tag, path);

            return this;
        }

        /// <summary>
        /// Turns the given expression into a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Command Dotted<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.Dotted(expression));
        }

        /// <summary>
        /// Turns the given expression into a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Command PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.PosFiltered(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Command PosAll<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.PosAll(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Command PosFirst<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.PosFirst(expression));
        }

        /// <summary>
        /// Turns the given expression into a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Command Elements<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.Elements(expression));
        }

        /// <summary>
        /// Turns the given index and expression into a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Command Elements<T>(int index, Expression<Func<T, object>> expression)
        {
            return Replace(Prop.Elements(index, expression));
        }

        /// <summary>
        /// Replaces the given tag in the template like "&lt;search_term&gt;" with the supplied value.
        /// </summary>
        /// <param name="tagName">The tag name without the surrounding &lt; and &gt;</param>
        /// <param name="replacementValue">The value to replace with</param>
        public Command Tag(string tagName, string replacementValue)
        {
            var tag = $"<{tagName}>";

            if(!tags.Contains(tag))
                missingTags.Add(tag);

            builder.Replace(tag, replacementValue);

            return this;
        }

        /// <summary>
        /// Executes the tag replacement and returns a string.
        /// <para>TIP: if the tags don't match fully, an exception will be thrown.</para>
        /// </summary>
        public new string ToString()
        {
            if (missingTags.Count > 0)
                throw new InvalidOperationException($"The following tags were missing from the template: [{string.Join(",", missingTags)}]");

            var output = builder.ToString();

            foreach (Match match in regex.Matches(output))
            {
                unReplacedTags.Add(match.Groups[0].Value);
            }

            if (unReplacedTags.Count > 0)
                throw new InvalidOperationException($"Replacements for the following tags are required: [{string.Join(",", unReplacedTags)}]");

            return output;
        }

        /// <summary>
        /// Executes the tag replacement and returns an array of pipeline stage definitions.
        /// <para>TIP: if the tags don't match fully, an exception will be thrown.</para>
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        public PipelineStageDefinition<TInput, TOutput>[] ToStages<TInput, TOutput>()
        {
            return BsonSerializer
                .Deserialize<BsonArray>(ToString())
                .Select(v => v.AsBsonDocument as PipelineStageDefinition<TInput, TOutput>)
                .ToArray();
        }

        /// <summary>
        /// Executes the tag replacement and returns a pipeline definition.
        /// <para>TIP: if the tags don't match fully, an exception will be thrown.</para>
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        public PipelineDefinition<TInput, TOutput> ToPipeline<TInput, TOutput>()
        {
            return ToStages<TInput, TOutput>();
        }
    }
}
