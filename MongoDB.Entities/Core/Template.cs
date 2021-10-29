using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
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
        /// <param name="template">The template string with tags for targeting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template) : base(template) { }
    }

    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    /// <typeparam name="TInput">The input type</typeparam>
    /// <typeparam name="TResult">The output type</typeparam>
    public class Template<TInput, TResult> : Template where TInput : IEntity
    {
        /// <summary>
        /// Initializes a template with a tagged input string.
        /// </summary>
        /// <param name="template">The template string with tags for targeting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template) : base(template) { }

        /// <summary>
        /// Gets the collection name of a given entity type and replaces matching tags in the template such as "&lt;EntityName&gt;"
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to get the collection name of</typeparam>
        public new Template<TInput, TResult> Collection<TEntity>() where TEntity : IEntity => base.Collection<TEntity>() as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given member expression (of input type) into a property name like "SomeProp" and replaces matching tags in the template such as "&lt;SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.RootProp.SomeProp</param>
        public Template<TInput, TResult> Property(Expression<Func<TInput, object>> expression) => base.Property(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given member expression (of output type) into a property name like "SomeProp" and replaces matching tags in the template such as "&lt;SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.RootProp.SomeProp</param>
        public Template<TInput, TResult> PropertyOfResult(Expression<Func<TResult, object>> expression) => base.Property(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given member expression (of any type) into a property name like "SomeProp" and replaces matching tags in the template such as "&lt;SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.RootProp.SomeProp</param>
        public new Template<TInput, TResult> Property<TOther>(Expression<Func<TOther, object>> expression) => base.Property(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the property paths in the given `new` expression (of input type) into names like "PropX &amp; PropY" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.PropX, x.Prop2.PropY }</param>
        public Template<TInput, TResult> Properties(Expression<Func<TInput, object>> expression) => base.Properties(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the property paths in the given `new` expression (of output type) into names like "PropX &amp; PropY" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.PropX, x.Prop2.PropY }</param>
        public Template<TInput, TResult> PropertiesOfResult(Expression<Func<TResult, object>> expression) => base.Properties(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the property paths in the given `new` expression (of any type) into paths like "PropX &amp; PropY" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.PropX, x.Prop2.PropY }</param>
        public new Template<TInput, TResult> Properties<TOther>(Expression<Func<TOther, object>> expression) => base.Properties(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given expression (of input type) to a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> Path(Expression<Func<TInput, object>> expression) => base.Path(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PathOfResult(Expression<Func<TResult, object>> expression) => base.Path(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of any type) to a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public new Template<TInput, TResult> Path<TOther>(Expression<Func<TOther, object>> expression) => base.Path(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the property paths in the given `new` expression (of input type) into paths like "Prop1.Child1 &amp; Prop2.Child2" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.Child1, x.Prop2.Child2 }</param>
        public Template<TInput, TResult> Paths(Expression<Func<TInput, object>> expression) => base.Paths(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the property paths in the given `new` expression (of output type) into paths like "Prop1.Child1 &amp; Prop2.Child2" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.Child1, x.Prop2.Child2 }</param>
        public Template<TInput, TResult> PathsOfResult(Expression<Func<TResult, object>> expression) => base.Paths(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the property paths in the given `new` expression (of any type) into paths like "Prop1.Child1 &amp; Prop2.Child2" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.Child1, x.Prop2.Child2 }</param>
        public new Template<TInput, TResult> Paths<TOther>(Expression<Func<TOther, object>> expression) => base.Paths(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given expression (of input type) to a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PosFiltered(Expression<Func<TInput, object>> expression) => base.PosFiltered(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PosFilteredOfResult(Expression<Func<TResult, object>> expression) => base.PosFiltered(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of any type) to a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public new Template<TInput, TResult> PosFiltered<TOther>(Expression<Func<TOther, object>> expression) => base.PosFiltered(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given expression (of input type) to a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PosAll(Expression<Func<TInput, object>> expression) => base.PosAll(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PosAllOfResult(Expression<Func<TResult, object>> expression) => base.PosAll(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of any type) to a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public new Template<TInput, TResult> PosAll<TOther>(Expression<Func<TOther, object>> expression) => base.PosAll(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given expression (of input type) to a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PosFirst(Expression<Func<TInput, object>> expression) => base.PosFirst(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template<TInput, TResult> PosFirstOfResult(Expression<Func<TResult, object>> expression) => base.PosFirst(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of any type) to a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public new Template<TInput, TResult> PosFirst<TOther>(Expression<Func<TOther, object>> expression) => base.PosFirst(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given expression (of input type) to a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<TInput, TResult> Elements(Expression<Func<TInput, object>> expression) => base.Elements(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of output type) to a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<TInput, TResult> ElementsOfResult(Expression<Func<TResult, object>> expression) => base.Elements(expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given expression (of any type) to a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public new Template<TInput, TResult> Elements<TOther>(Expression<Func<TOther, object>> expression) => base.Elements(expression) as Template<TInput, TResult>;



        /// <summary>
        /// Turns the given index and expression (of input type) to a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<TInput, TResult> Elements(int index, Expression<Func<TInput, object>> expression) => base.Elements(index, expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given index and expression (of output type) to a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template<TInput, TResult> ElementsOfResult(int index, Expression<Func<TResult, object>> expression) => base.Elements(index, expression) as Template<TInput, TResult>;

        /// <summary>
        /// Turns the given index and expression (of any type) to a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public new Template<TInput, TResult> Elements<TOther>(int index, Expression<Func<TOther, object>> expression) => base.Elements(index, expression) as Template<TInput, TResult>;



        /// <summary>
        /// Replaces the given tag in the template like "&lt;search_term&gt;" with the supplied value.
        /// </summary>
        /// <param name="tagName">The tag name without the surrounding &lt; and &gt;</param>
        /// <param name="replacementValue">The value to replace with</param>
        public new Template<TInput, TResult> Tag(string tagName, string replacementValue) => base.Tag(tagName, replacementValue) as Template<TInput, TResult>;

        /// <summary>
        /// Executes the tag replacement and returns a pipeline definition.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public PipelineDefinition<TInput, TResult> ToPipeline() => ToPipeline<TInput, TResult>();

        /// <summary>
        /// Executes the tag replacement and returns array filter definitions.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public IEnumerable<ArrayFilterDefinition> ToArrayFilters() => ToArrayFilters<TInput>();
    }

    /// <summary>
    /// A helper class to build a JSON command from a string with tag replacement
    /// </summary>
    public class Template
    {
        private static readonly Regex regex = new("<.*?>", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<int, string> cache = new();

        internal readonly StringBuilder builder;
        private bool cacheHit, hasAppendedStages;
        private readonly int cacheKey;
        private readonly HashSet<string> goalTags = new();
        private readonly HashSet<string> missingTags = new();
        private readonly HashSet<string> replacedTags = new();
        private readonly Dictionary<string, string> valueTags = new();

        /// <summary>
        /// Initialize a command builder with the supplied template string.
        /// </summary>
        /// <param name="template">The template string with tags for targeting replacements such as "&lt;Author.Name&gt;"</param>
        public Template(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentException("Unable to instantiate a template from an empty string!");

            cacheKey = template.GetHashCode();

            cache.TryGetValue(cacheKey, out var cachedTemplate);

            if (cachedTemplate != null)
            {
                cacheHit = true;
                builder = new StringBuilder(cachedTemplate);
            }
            else
            {
                builder = new StringBuilder(template.Trim());
            }

            if (!(builder[0] == '[' && builder[1] == ']')) //not an empty array
            {
                foreach (Match match in regex.Matches(cacheHit ? cachedTemplate : template))
                    goalTags.Add(match.Value);

                if (!cacheHit && goalTags.Count == 0)
                    throw new ArgumentException("No replacement tags such as '<tagname>' were found in the supplied template string");
            }
        }

        private Template ReplacePath(string path)
        {
            var tag = $"<{path}>";

            if (!goalTags.Contains(tag))
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
        /// Appends a pipeline stage json string to the current pipeline. 
        /// This method can only be used if the template was initialized with an array of pipeline stages. 
        /// If this is going to be the first stage of your pipeline, you must instantiate the template with an empty array string <c>new Template("[]")</c>
        /// <para>WARNING: Appending stages prevents this template from being cached!!!</para>
        /// </summary>
        /// <param name="pipelineStageString">The pipeline stage json string to append</param>
        public void AppendStage(string pipelineStageString)
        {
            hasAppendedStages = true;

            int pipelineEndPos = 0;
            int lastCharPos = builder.Length - 1;

            if (builder[lastCharPos] == ']')
                pipelineEndPos = lastCharPos;

            if (pipelineEndPos == 0)
            {
                throw new InvalidOperationException(
                    "Stages can only be appended to a template initialized with an array of stages. " +
                    "Initialize the template with an empty array \"[]\" if this is the first stage.");
            }

            if (!pipelineStageString.StartsWith("{") && !pipelineStageString.EndsWith("}"))
                throw new ArgumentException("A pipeline stage string must begin with a { and end with a }");

            foreach (Match match in regex.Matches(pipelineStageString))
                goalTags.Add(match.Value);

            if (builder[0] == '[' && builder[1] == ']')//empty array
                builder.Remove(lastCharPos, 1);
            else
                builder[lastCharPos] = ',';

            builder
                .Append(pipelineStageString)
                .Append(']');
        }

        /// <summary>
        /// Gets the collection name of a given entity type and replaces matching tags in the template such as "&lt;EntityName&gt;"
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to get the collection name of</typeparam>
        public Template Collection<TEntity>() where TEntity : IEntity
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.Collection<TEntity>());
        }

        /// <summary>
        /// Turns the given member expression into a property name like "SomeProp" and replaces matching tags in the template such as "&lt;SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.RootProp.SomeProp</param>
        public Template Property<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.Property(expression));
        }

        /// <summary>
        /// Turns the property paths in the given `new` expression into property names like "PropX &amp; PropY" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.PropX, x.Prop2.PropY }</param>
        public Template Properties<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            var props =
                (expression.Body as NewExpression)?
                .Arguments
                .Cast<MemberExpression>()
                .Select(e => e.Member.Name);

            if (!props.Any())
                throw new ArgumentException("Unable to parse any property names from the supplied `new` expression!");

            foreach (var p in props)
                ReplacePath(p);

            return this;
        }

        /// <summary>
        /// Turns the given expression into a dotted path like "SomeList.SomeProp" and replaces matching tags in the template such as "&lt;SomeList.SomeProp&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template Path<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.Path(expression));
        }

        /// <summary>
        /// Turns the property paths in the given `new` expression into paths like "Prop1.Child1 &amp; Prop2.Child2" and replaces matching tags in the template.
        /// </summary>
        /// <param name="expression">x => new { x.Prop1.Child1, x.Prop2.Child2 }</param>
        public Template Paths<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            var paths =
                (expression.Body as NewExpression)?
                .Arguments
                .Select(a => Prop.GetPath(a.ToString()));

            if (!paths.Any())
                throw new ArgumentException("Unable to parse any property paths from the supplied `new` expression!");

            foreach (var p in paths)
                ReplacePath(p);

            return this;
        }

        /// <summary>
        /// Turns the given expression into a positional filtered path like "Authors.$[a].Name" and replaces matching tags in the template such as "&lt;Authors.$[a].Name&gt;"
        /// <para>TIP: Index positions start from [0] which is converted to $[a] and so on.</para>
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosFiltered<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.PosFiltered(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the all positional operator like "Authors.$[].Name" and replaces matching tags in the template such as "&lt;Authors.$[].Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosAll<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.PosAll(expression));
        }

        /// <summary>
        /// Turns the given expression into a path with the first positional operator like "Authors.$.Name" and replaces matching tags in the template such as "&lt;Authors.$.Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeList[0].SomeProp</param>
        public Template PosFirst<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.PosFirst(expression));
        }

        /// <summary>
        /// Turns the given expression into a path without any filtered positional identifier prepended to it like "Name" and replaces matching tags in the template such as "&lt;Name&gt;"
        /// </summary>
        /// <param name="expression">x => x.SomeProp</param>
        public Template Elements<T>(Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

            return ReplacePath(Prop.Elements(expression));
        }

        /// <summary>
        /// Turns the given index and expression into a path with the filtered positional identifier prepended to the property path like "a.Name" and replaces matching tags in the template such as "&lt;a.Name&gt;"
        /// </summary>
        /// <param name="index">0=a 1=b 2=c 3=d and so on...</param>
        /// <param name="expression">x => x.SomeProp</param>
        public Template Elements<T>(int index, Expression<Func<T, object>> expression)
        {
            if (cacheHit) return this;

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

            if (!goalTags.Contains(tag))
            {
                missingTags.Add(tag);
            }
            else
            {
                valueTags[tag] = replacementValue;
            }

            return this;
        }

        /// <summary>
        /// Executes the tag replacement and returns a string.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public string RenderToString()
        {
            if (!cacheHit && !hasAppendedStages)
            {
                cache[cacheKey] = builder.ToString();
                cacheHit = true; //in case this method is called multiple times
            }

            foreach (var t in valueTags.ToArray())
            {
                builder.Replace(t.Key, t.Value);
                replacedTags.Add(t.Key);
            }

            if (missingTags.Count > 0)
                throw new InvalidOperationException($"The following tags were missing from the template: [{string.Join(",", missingTags)}]");

            var unReplacedTags = goalTags.Except(replacedTags);

            if (unReplacedTags.Any())
                throw new InvalidOperationException($"Replacements for the following tags are required: [{string.Join(",", unReplacedTags)}]");

            return builder.ToString();
        }

        [Obsolete("Please use the `RenderToString` method instead of `ToString`", true)]
        public new string ToString()
        {
            throw new InvalidOperationException("Please use the `RenderToString` method instead of `ToString`");
        }

        /// <summary>
        /// Executes the tag replacement and returns the pipeline stages as an array of BsonDocuments.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public IEnumerable<BsonDocument> ToStages()
        {
            return BsonSerializer
                .Deserialize<BsonArray>(RenderToString())
                .Select(v => v.AsBsonDocument);
        }

        /// <summary>
        /// Executes the tag replacement and returns a pipeline definition.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        public PipelineDefinition<TInput, TOutput> ToPipeline<TInput, TOutput>()
        {
            return ToStages().ToArray();
        }

        /// <summary>
        /// Executes the tag replacement and returns array filter definitions.
        /// <para>TIP: if all the tags don't match, an exception will be thrown.</para>
        /// </summary>
        public IEnumerable<ArrayFilterDefinition> ToArrayFilters<T>()
        {
            return BsonSerializer
                .Deserialize<BsonArray>(RenderToString())
                .Select(v => (ArrayFilterDefinition<T>)v.AsBsonDocument);
        }
    }
}
