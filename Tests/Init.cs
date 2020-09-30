using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public static class InitTest
    {
        [AssemblyInitialize]
        public static async Task Init(TestContext _)
        {
            await DB.InitAsync("mongodb-entities-test");
        }
    }
}