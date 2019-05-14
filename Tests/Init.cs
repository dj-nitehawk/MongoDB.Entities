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

            DB.DefineTextIndexAsync<Author>(
               "my_text_index",
               true,
              //a => a.Renamed,
               a => a.Name).Wait();
        }
    }
}