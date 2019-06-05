using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class GeoNearTest
    {
        [TestMethod]
        public void geo_near_returns_correct_results()
        {
            DB.Index<Place>()
                .Key(x => x.Location, KeyType.Geo2DSphere)
                .Option(x => x.Background = false)
                .Create();

            (new[]
            {
                new Place { Name = "Paris", Location = new Coordinates2D(48.8539241, 2.2913515) },
                new Place { Name = "Versailles", Location = new Coordinates2D(48.796964, 2.137456) },
                new Place { Name = "Poissy", Location = new Coordinates2D(48.928860, 2.046889) }
            })
            .Save();

            var res = new GeoNear<Place>(
                            NearCoordinates: new Coordinates2D(48.857908, 2.295243), //eiffel tower
                            DistanceField: x => x.DistanceKM,
                            MaxDistance: 20000)
                          .Fluent().ToList();
        }
    }
}
