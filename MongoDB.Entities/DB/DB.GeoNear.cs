using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Start a fluent aggregation pipeline with a $GeoNear stage with the supplied parameters
    /// </summary>
    /// <param name="NearCoordinates">The coordinates from which to find documents from</param>
    /// <param name="DistanceField">x => x.Distance</param>
    /// <param name="Spherical">Calculate distances using spherical geometry or not</param>
    /// <param name="MaxDistance">The maximum distance in meters from the center point that the documents can be</param>
    /// <param name="MinDistance">The minimum distance in meters from the center point that the documents can be</param>
    /// <param name="Limit">The maximum number of documents to return</param>
    /// <param name="Query">Limits the results to the documents that match the query</param>
    /// <param name="DistanceMultiplier">The factor to multiply all distances returned by the query</param>
    /// <param name="IncludeLocations">Specify the output field to store the point used to calculate the distance</param>
    /// <param name="IndexKey"></param>
    /// <param name="options">The options for the aggregation. This is not required.</param>
    /// <typeparam name="T">The type of entity</typeparam>
    public IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates,
                                          Expression<Func<T, object?>> DistanceField,
                                          bool Spherical = true,
                                          int? MaxDistance = null,
                                          int? MinDistance = null,
                                          int? Limit = null,
                                          BsonDocument? Query = null,
                                          int? DistanceMultiplier = null,
                                          Expression<Func<T, object?>>? IncludeLocations = null,
                                          string? IndexKey = null,
                                          AggregateOptions? options = null) where T : IEntity
    {
        var globalFilter = Logic.MergeWithGlobalFilter(IgnoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);

        var pipeline = new GeoNear<T>
            {
                near = NearCoordinates,
                distanceField = DistanceField.FullPath(),
                spherical = Spherical,
                maxDistance = MaxDistance,
                minDistance = MinDistance,
                query = Query,
                distanceMultiplier = DistanceMultiplier,
                limit = Limit,
                includeLocs = IncludeLocations?.FullPath(),
                key = IndexKey
            }
            .ToFluent(this, options, Session);

        return globalFilter != Builders<T>.Filter.Empty
                   ? pipeline.Match(globalFilter)
                   : pipeline;
    }
}