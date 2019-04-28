using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public static class InitTest
    {
        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            new DB("mongodb-entities-test");
            DB.Collection<Book>().Count();
        }
    }
}