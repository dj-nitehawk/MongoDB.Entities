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
    /// <typeparam name="T">Any type that implements IEntity</typeparam>
    public class Template<T> : Template<T, T> where T : IEntity
    {
        /// <summary>
        /// Initializes a template with a tagged input string.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template) : base(template) { }
    }

    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    /// <typeparam name="TResult">The output type</typeparam>
    public class Template<T, TResult> : Template where T : IEntity
    {
        /// <summary>
        /// Initializes a template with a tagged input string.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template) : base(template) { }

        /// <summary>
        /// Turns the given expression (of input type) to a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> Path(Expression<Func<T, object>> expression) => base.Path(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PathOfResult(Expression<Func<TResult, object>> expression) => base.Path(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of input type) to a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosFiltered(Expression<Func<T, object>> expression) => base.PosFiltered(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosFilteredOfResult(Expression<Func<TResult, object>> expression) => base.PosFiltered(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of input type) to a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosAll(Expression<Func<T, object>> expression) => base.PosAll(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosAllOfResult(Expression<Func<TResult, object>> expression) => base.PosAll(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of input type) to a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosFirst(Expression<Func<T, object>> expression) => base.PosFirst(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<T, TResult> PosFirstOfResult(Expression<Func<TResult, object>> expression) => base.PosFirst(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of input type) to a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<T, TResult> Elements(Expression<Func<T, object>> expression) => base.Elements(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<T, TResult> ElementsOfResult(Expression<Func<TResult, object>> expression) => base.Elements(expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given index and expression (of input type) to a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<T, TResult> Elements(int index, Expression<Func<T, object>> expression) => base.Elements(index, expression) as Template<T, TResult>;

        /// <summary>
        /// Turns the given index and expression (of output type) to a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<T, TResult> ElementsOfResult(int index, Expression<Func<TResult, object>> expression) => base.Elements(index, expression) as Template<T, TResult>;

        /// <summary>
        /// Replaces the given tag in the template like "&lt;search_term&gt;" with the supplied value.
        /// </summary>
        /// <param name="tagName">The tag name without the surrounding &lt; and &gt;</param>
        /// <param name="replacementValue">The value to replace with</param>
        public new Template<T, TResult> Tag(string tagName, string replacementValue) => base.Tag(tagName, replacementValue) as Template<T, TResult>;

        /// <summary>
        /// Executes the tag replacement and returns a pipeline definition.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public PipelineDefinition<T, TResult> ToPipeline() => base.ToPipeline<T, TResult>();

        /// <summary>
        /// Executes the tag replacement and returns array filter definitions.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public IEnumerable<ArrayFilterDefinition> ToArrayFilters() => base.ToArrayFilters<T>();
    }

    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    public class Template
    {
        private static readonly Regex regex = new Regex("<.*?>", RegexOptions.Compiled);
        private readonly StringBuilder builder;
        private readonly HashSet<string> tags, missingTags, replacedTags;

        /// <summary>
        /// Initialize a command builder with the supplied template string.
        /// </summary>
        /// <param name="template">The template string with tags for targetting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template)
        {
            builder = new StringBuilder(template, template.Length);
            tags = new HashSet<string>();
            missingTags = new HashSet<string>();
            replacedTags = new HashSet<string>();

            foreach (Match match in regex.Matches(template))
            {
                tags.Add(match.Value);
            }

            if (tags.Count == 0)
                throw new ArgumentException("No replacement tags such as '<tagname>' were found in the supplied template string");
        }

        private Template ReplacePath(string path)
        {
            var tag = $"<{path}>";

            if (!tags.Contains(tag))
            {
                missingTags.Add(tag);
            }
            else
            {
                builder.Replace(tag, path);
                replacedTags.Add(tag);
            }

            return this;
        }

        /// <summary>
        /// Turns the given expression into a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template Path<T>(Expression<Func<T, object>> expression)
        {
            return ReplacePath(Prop.Path(expression));
        }

        /// <summary>
        /// Turns the given expression into a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            return ReplacePath(Prop.PosFiltered(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosAll<T>(Expression<Func<T, object>> expression)
        {
            return ReplacePath(Prop.PosAll(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosFirst<T>(Expression<Func<T, object>> expression)
        {
            return ReplacePath(Prop.PosFirst(expression));
        }

        /// <summary>
        /// Turns the given expression into a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template Elements<T>(Expression<Func<T, object>> expression)
        {
            return ReplacePath(Prop.Elements(expression));
        }

        /// <summary>
        /// Turns the given index and expression into a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template Elements<T>(int index, Expression<Func<T, object>> expression)
        {
            return ReplacePath(Prop.Elements(index, expression));
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
            {
                missingTags.Add(tag);
            }
            else
            {
                builder.Replace(tag, replacementValue);
                replacedTags.Add(tag);
            }

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

            var unReplacedTags = tags.Except(replacedTags);

            if (unReplacedTags.Any())
                throw new InvalidOperationException($"Replacements for the following tags are required: [{string.Join(",", unReplacedTags)}]");

            return builder.ToString();
        }

        /// <summary>
        /// Executes the tag replacement and returns the pipeline stages as an array of BsonDocuments.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public BsonDocument[] ToStages()
        {
            return BsonSerializer
                .Deserialize<BsonArray>(ToString())
                .Select(v => v.AsBsonDocument)
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
            return ToStages();
        }

        /// <summary>
        /// Executes the tag replacement and returns array filter definitions.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public IEnumerable<ArrayFilterDefinition> ToArrayFilters<T>()
        {
            return BsonSerializer
                .Deserialize<BsonArray>(ToString())
                .Select(v => (ArrayFilterDefinition<T>)v.AsBsonDocument);
        }
    }
}
