using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public static class InitTest
    {
        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            new DB("mongodb-entities-test");
        }
    }
}

//db.setProfilingLevel(2)
//db.system.profile.find().limit(3).sort( { ts : -1 } ).pretty()