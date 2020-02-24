using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Entities
{
    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    public class Template<T> : Template<T, T> where T : IEntity
    {
        /// <summary>
        /// Initialize a command builder with the supplied template string.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template) : base(template) { }
    }

    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    public class Template<T, TResult> : Template where T : IEntity
    {
        /// <summary>
        /// Initialize a command builder with the supplied template string.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template) : base(template) { }

        /// <summary>
        /// Turns the given expression into a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> Dotted(Expression<Func<T, object>> expression) => base.Dotted(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression into a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosFiltered(Expression<Func<T, object>> expression) => base.PosFiltered(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression into a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosAll(Expression<Func<T, object>> expression) => base.PosAll(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression into a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosFirst(Expression<Func<T, object>> expression) => base.PosFirst(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression into a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<T, TResult> Elements(Expression<Func<T, object>> expression) => base.Elements(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given index and expression into a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<T, TResult> Elements(int index, Expression<Func<T, object>> expression) => base.Elements(index, expression) as Template<T, TResult>;

        /// <summary>
        /// Replaces the given tag in the template like "&lt;search_term&gt;" with the supplied value.
        /// </summary>
        /// <param name="tagName">The tag name without the surrounding &lt; and &gt;</param>
        /// <param name="replacementValue">The value to replace with</param>
        public new Template<T, TResult> Tag(string tagName, string replacementValue) => base.Tag(tagName, replacementValue) as Template<T, TResult>;

        /// <summary>
        /// Executes the tag replacement and returns a string.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public PipelineStageDefinition<T, TResult>[] ToStages() => ToStages<T, TResult>();

        /// <summary>
        /// Executes the tag replacement and returns a pipeline definition.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        public PipelineDefinition<T, TResult> ToPipeline() => ToPipeline<T, TResult>();
    }


    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    public class Template
    {
        private static readonly Regex regex = new Regex("<.*?>", RegexOptions.Compiled);
        private readonly StringBuilder builder;
        private readonly HashSet<string> tags, missingTags, unReplacedTags;

        /// <summary>
        /// Initialize a command builder with the supplied template string.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template)
        {
            builder = new StringBuilder(template, template.Length);
            tags = new HashSet<string>();
            missingTags = new HashSet<string>();
            unReplacedTags = new HashSet<string>();

            foreach (Match match in regex.Matches(template))
            {
                tags.Add(match.Value);
            }

            if (tags.Count == 0)
                throw new ArgumentException("No replacement tags such as '<tagname>' were found in the supplied template string");
        }

        private Template Replace(string path)
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
        public Template Dotted<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.Dotted(expression));
        }

        /// <summary>
        /// Turns the given expression into a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.PosFiltered(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosAll<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.PosAll(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosFirst<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.PosFirst(expression));
        }

        /// <summary>
        /// Turns the given expression into a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template Elements<T>(Expression<Func<T, object>> expression)
        {
            return Replace(Prop.Elements(expression));
        }

        /// <summary>
        /// Turns the given index and expression into a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template Elements<T>(int index, Expression<Func<T, object>> expression)
        {
            return Replace(Prop.Elements(index, expression));
        }

        /// <summary>
        /// Replaces the given tag in the template like "&lt;search_term&gt;" with the supplied value.
        /// </summary>
        /// <param name="tagName">The tag name without the surrounding &lt; and &gt;</param>
        /// <param name="replacementValue">The value to replace with</param>
        public Template Tag(string tagName, string replacementValue)
        {
            var tag = $"<{tagName}>";

            if (!tags.Contains(tag))
                missingTags.Add(tag);

            builder.Replace(tag, replacementValue);

            return this;
        }

        /// <summary>
        /// Executes the tag replacement and returns a string.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public new string ToString()
        {
            if (missingTags.Count > 0)
                throw new InvalidOperationException($"The following tags were missing from the template: [{string.Join(",", missingTags)}]");

            var output = builder.ToString();

            foreach (Match match in regex.Matches(output))
            {
                unReplacedTags.Add(match.Value);
            }

            if (unReplacedTags.Count > 0)
                throw new InvalidOperationException($"Replacements for the following tags are required: [{string.Join(",", unReplacedTags)}]");

            return output;
        }

        /// <summary>
        /// Executes the tag replacement and returns an array of pipeline stage definitions.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        public PipelineStageDefinition<TInput, TOutput>[] ToStages<TInput, TOutput>()
        {
            return BsonSerializer
                .Deserialize<BsonArray>(ToString())
                .Select(v => (PipelineStageDefinition<TInput, TOutput>)v.AsBsonDocument)
                .ToArray();
        }

        /// <summary>
        /// Executes the tag replacement and returns a pipeline definition.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        public PipelineDefinition<TInput, TOutput> ToPipeline<TInput, TOutput>()
        {
            return ToStages<TInput, TOutput>();
        }
    }
}
