using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class Migrations
    {
        [TestMethod]
        public void MyTestMethod()
        {
            DB.Migrate();
        }
    }
}
