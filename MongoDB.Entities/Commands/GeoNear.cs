using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a GeoJsonPoint of GeoJson2DGeographicCoordinates
    /// </summary>
    public class Coordinates2D
    {
        public string type { get; set; }
        public double[] coordinates { get; set; }

        /// <summary>
        /// Instantiate a new Coordinates2D instance with the supplied longtitude and latitude
        /// </summary>
        public Coordinates2D(double longitude, double latitude)
        {
            type = "Point";
            coordinates = new[] { longitude, latitude };
        }

        /// <summary>
        /// Converts a Coordinates2D instance to a GeoJsonPoint of GeoJson2DGeographicCoordinates 
        /// </summary>
        /// <returns></returns>
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> ToGeoJsonPoint()
        {
            return GeoJson.Point(GeoJson.Geographic(coordinates[0], coordinates[1]));
        }
    }

    /// <summary>
    /// Represents a $GeoNear fluent aggregation pipeline
    /// </summary>
    /// <typeparam name="T">Any class that inherits from Entity</typeparam>
    public class GeoNear<T> where T : Entity
    {
        public Coordinates2D near { get; set; }
        public bool spherical { get; set; }
        public int limit { get; set; }
        public int? maxDistance { get; set; }
        public BsonDocument query { get; set; }
        public int? distanceMultiplier { get; set; }
        public string distanceField { get; set; }
        //public string includeLocs { get; set; }
        public int? minDistance { get; set; }
        //public string key { get; set; }

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
        public GeoNear(
            Coordinates2D NearCoordinates,
            Expression<Func<T, object>> DistanceField,
            bool Spherical = true,
            int? MaxDistance = null,
            int? MinDistance = null,
            int Limit = 100,
            BsonDocument Query = null,
            int? DistanceMultiplier = null)
            //string IncludeLocations = "",
            //string IndexKey = "")
        {
            near = NearCoordinates;
            distanceField = PropertyName(DistanceField);
            spherical = Spherical;
            maxDistance = MaxDistance;
            minDistance = MinDistance;
            query = Query == null ? new BsonDocument() : Query;
            distanceMultiplier = DistanceMultiplier;
            limit = Limit;
            //includeLocs = IncludeLocations;
            //key = IndexKey;
        }

        /// <summary>
        /// Returns an IAggregateFluent query pipeline ready for further processing.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public IAggregateFluent<T> Fluent(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            var stage = new BsonDocument { { "$geoNear", this.ToBsonDocument() } };

            return session == null
                    ? DB.Collection<T>().Aggregate(options).AppendStage<T>(stage)
                    : DB.Collection<T>().Aggregate(session, options).AppendStage<T>(stage);
        }

        private string PropertyName(Expression<Func<T, object>> property)
        {
            if (!(property.Body is MemberExpression member)) member = (property.Body as UnaryExpression)?.Operand as MemberExpression;
            if (member == null) throw new ArgumentException("Unable to get property name");
            return member.Member.Name;
        }

    }
}
