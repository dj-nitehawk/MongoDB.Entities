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