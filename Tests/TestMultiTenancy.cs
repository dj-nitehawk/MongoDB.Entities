using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class MultiTenancy
    {
        private readonly string[] tenants = new[] { "TenantOne", "TenantTwo", "TenantThree", "TenantFour", "TenantFive" };
        private readonly Random random = new();

        private string RandomTenantPrefix()
            => tenants[random.Next(tenants.Length)];

        private class MyTenantContext : TenantContext
        {
            public MyTenantContext(string tenantPrefix)
            {
                DB.DatabaseFor<Author>("test-data");

                SetTenantPrefix(tenantPrefix);
                Init("test-data", "localhost");
                ModifiedBy = new() { UserID = "xxx" };
            }
        }

        private static Task Init(string tenantPrefix)
        {
            var db = new MyTenantContext(tenantPrefix);

            var list = new List<Author>();

            for (int i = 1; i <= 25; i++)
            {
                list.Add(new Author { Name = tenantPrefix, Age = 111 });
            }

            for (int i = 1; i <= 10; i++)
            {
                list.Add(new Author { Name = tenantPrefix, Age = 222 });
            }

            return db.SaveAsync(list);
        }

        [TestMethod]
        public async Task count_estimated_works()
        {
            var tenantPrefix = RandomTenantPrefix();
            var db = new MyTenantContext(tenantPrefix);
            await DB.Database<Author>(tenantPrefix).Client.DropDatabaseAsync($"{tenantPrefix}~test-data");
            await Init(tenantPrefix);

            var count = await db.CountEstimatedAsync<Author>();
            Assert.IsTrue(count > 0);

            //await DB.Database<Author>(tenantPrefix).Client.DropDatabaseAsync($"{tenantPrefix}~test-data");
        }
    }
}