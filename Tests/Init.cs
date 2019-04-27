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
            new DB("mongotest");
            DB.Collection<Book>().Count();
        }
    }
}