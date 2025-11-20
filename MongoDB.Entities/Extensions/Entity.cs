using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Extension methods for entities
/// </summary>
public static partial class Extensions
{
    class Holder<T>(T data)
    {
        public T Data { get; } = data;
    }

    static T Duplicate<T>(this T source)
        => BsonSerializer.Deserialize<Holder<T>>(new Holder<T>(source).ToBson()).Data;

    internal static void ThrowIfUnsaved(this object? entityID)
    {
        if (entityID == null)
            throw new InvalidOperationException("Please save the entity before performing this operation!");
    }

    internal static void ThrowIfUnsaved<T>(this T entity) where T : IEntity
    {
        if (entity.HasDefaultID())
            throw new InvalidOperationException("Please save the entity before performing this operation!");
    }

    /// <summary>
    /// Extension method for processing collections in batches with streaming (yield return)
    /// </summary>
    /// <typeparam name="T">The type of the objects inside the source collection</typeparam>
    /// <param name="collection">The source collection</param>
    /// <param name="batchSize">The size of each batch</param>
    public static IEnumerable<IEnumerable<T>> ToBatches<T>(this IEnumerable<T> collection, int batchSize = 100)
    {
        var batch = new List<T>(batchSize);

        foreach (var item in collection)
        {
            batch.Add(item);

            if (batch.Count != batchSize)
                continue;

            yield return batch;

            batch.Clear();
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Returns the full dotted path of a property for the given expression
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static string FullPath<T>(this Expression<Func<T, object?>> expression)
        => Prop.Path(expression);

    extension<T>(T _) where T : IEntity
    {
        /// <summary>
        /// An IQueryable collection of sibling Entities.
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="options"></param>
        public IQueryable<T> Queryable(DB? db = null, AggregateOptions? options = null)
            => DB.InstanceOrDefault(db).Queryable<T>(options);

        /// <summary>
        /// Creates an unlinked duplicate of the original IEntity ready for embedding with a blank ID.
        /// </summary>
        public T ToDocument(DB? dbInstance = null)
        {
            var res = _.Duplicate();
            res.SetId(res.GenerateNewID());

            return res;
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<ulong> NextSequentialNumberAsync(DB? db = null, CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).NextSequentialNumberAsync<T>(cancellation);
    }

    /// <summary>
    /// Creates unlinked duplicates of the original Entities ready for embedding with blank IDs.
    /// </summary>
    public static T[] ToDocuments<T>(this T[] entities, DB? dbInstance = null) where T : IEntity
    {
        var res = entities.Duplicate();
        foreach (var e in res)
            e.SetId(e.GenerateNewID());

        return res;
    }

    /// <summary>
    /// Creates unlinked duplicates of the original Entities ready for embedding with blank IDs.
    /// </summary>
    public static IEnumerable<T> ToDocuments<T>(this IEnumerable<T> entities, DB? dbInstance = null) where T : IEntity
    {
        var res = entities.Duplicate();
        foreach (var e in res)
            e.SetId(e.GenerateNewID());

        return res;
    }

    /// <summary>
    /// Sort a list of objects by relevance to a given string using Levenshtein Distance
    /// </summary>
    /// <typeparam name="T">Any object type</typeparam>
    /// <param name="objects">The list of objects to sort</param>
    /// <param name="searchTerm">The term to measure relevance to</param>
    /// <param name="propertyToSortBy">x => x.PropertyName [the term will be matched against the value of this property]</param>
    /// <param name="maxDistance">The maximum levenstein distance to qualify an item for inclusion in the returned list</param>
    public static IEnumerable<T> SortByRelevance<T>(this IEnumerable<T> objects, string searchTerm, Func<T, string> propertyToSortBy, int? maxDistance = null)
    {
        var lev = new Levenshtein(searchTerm);

        var res = objects.Select(
            o => new
            {
                score = lev.DistanceFrom(propertyToSortBy(o)),
                obj = o
            });

        if (maxDistance.HasValue)
            res = res.Where(x => x.score <= maxDistance.Value);

        return res.OrderBy(x => x.score)
                  .Select(x => x.obj);
    }

    /// <summary>
    /// Converts a search term to Double Metaphone hash code suitable for fuzzy text searching.
    /// </summary>
    /// <param name="term">A single or multiple word search term</param>
    public static string ToDoubleMetaphoneHash(this string term)
    {
        return string.Join(" ", DoubleMetaphone.GetKeys(RemoveDiacritics(term)));

        static string RemoveDiacritics(string text)
        {
            var sb = new StringBuilder();
            var str = text.Normalize(NormalizationForm.FormD);

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}