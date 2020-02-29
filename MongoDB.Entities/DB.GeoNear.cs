using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Linq.Expressions;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Start a fluent aggregation pipeline with a $GeoNear stage with the supplied parameters.
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
        /// <param name="session">An optional session if using within a transaction</param>
        public static IAggregateFluent<T> FluentGeoNear<T>(Coordinates2D NearCoordinates, Expression<Func<T, object>> DistanceField, bool Spherical = true, double? MaxDistance = null, double? MinDistance = null, int? Limit = null, BsonDocument Query = null, double? DistanceMultiplier = null, Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null, AggregateOptions options = null, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return (new GeoNear<T>
            {
                near = NearCoordinates,
                distanceField = DistanceField?.FullPath(),
                spherical = Spherical,
                maxDistance = MaxDistance,
                minDistance = MinDistance,
                query = Query,
                distanceMultiplier = DistanceMultiplier,
                limit = Limit,
                includeLocs = IncludeLocations?.FullPath(),
                key = IndexKey,
            })
            .ToFluent(options, session, db);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $GeoNear stage with the supplied parameters.
        /// </summary>
        /// <param name="NearCoordinates">The coordinates from which to find documents from</param>
        /// <param name="DistanceField">x => x.Distance</param>
        /// <param name="Spherical">Calculate distances using spherical geometry or not</param>
        /// <param name="MaxDistance">The maximum distance from the center point that the documents can be</param>
        /// <param name="MinDistance">The minimum distance from the center point that the documents can be</param>
        /// <param name="Limit">The maximum number of documents to return</param>
        /// <param name="Query">Limits the results to the documents that match the query</param>
        /// <param name="DistanceMultiplier">The factor to multiply all distances returned by the query</param>
        /// <param name="IncludeLocations">Specify the output field to store the point used to calculate the distance</param>
        /// <param name="IndexKey"></param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public IAggregateFluent<T> FluentGeoNear<T>(Coordinates2D NearCoordinates, Expression<Func<T, object>> DistanceField, bool Spherical = true, double? MaxDistance = null, double? MinDistance = null, int? Limit = null, BsonDocument Query = null, double? DistanceMultiplier = null, Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null, AggregateOptions options = null, IClientSessionHandle session = null) where T : IEntity
        {
            return FluentGeoNear(NearCoordinates, DistanceField, Spherical, MaxDistance, MinDistance, Limit, Query, DistanceMultiplier, IncludeLocations, IndexKey, options, session, DbName);
        }
    }
}
