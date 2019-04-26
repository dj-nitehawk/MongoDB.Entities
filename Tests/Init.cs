using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Entities;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public static class InitTest
    {
        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            new DB("mongotest");
        }
    }
}