using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoDB.Entities.Tests;

[TestClass]
public class MappedUpdates
{
    static MappedUpdates()
    {
        ConventionRegistry.Register("MappedUpdates", new ConventionPack { new IgnoreTransientConvention() }, t => t == typeof(MappedAccount));
        BsonClassMap.RegisterClassMap<AccountBase>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(a => a.Key);
            cm.UnmapMember(a => a.InheritedSecret);
            cm.MapMember(a => a.Name).SetElementName("name");
        });
        BsonClassMap.RegisterClassMap<MappedAccount>(cm =>
        {
            cm.AutoMap();
            cm.UnmapMember(a => a.Secret);
            cm.MapMember(a => a.Score).SetElementName("score").SetSerializer(new Int32Serializer(BsonType.String));
        });
    }

    public static IEnumerable<object[]> WriteCases()
    {
        foreach (var operation in new[]
                 {
                     "save-only", "save-only-names", "batch-only", "batch-only-names",
                     "save-except", "save-except-names", "batch-except", "batch-except-names", "preserving",
                     "update-with", "update-only", "update-except", "get-with", "get-only", "get-except"
                 })
        {
            foreach (var mode in new[] { "direct", "commit", "abort" })
                yield return new object[] { operation, mode };
        }
    }

    [TestMethod]
    [DynamicData(nameof(WriteCases))]
    public async Task entity_writes_honor_mapped_members(string operation, string mode)
    {
        var db = await InitTest.InitTestDatabase("mapped_updates_" + Guid.NewGuid().ToString("N"));
        try
        {
            var accounts = new[] { new MappedAccount(), new MappedAccount() };
            await db.SaveAsync(accounts);
            var raw = db.Database().GetCollection<BsonDocument>(db.CollectionName<MappedAccount>());
            foreach (var account in accounts)
                AssertDocument(await Read(raw, account.Key), account.Key, "before", "1", "keep");

            foreach (var account in accounts)
            {
                account.Name = "after";
                account.Score = 2;
                account.Keep = "changed";
            }

            if (mode == "direct")
                await Write(db, operation, accounts);
            else
            {
                using var transaction = db.Transaction();
                await Write(transaction, operation, accounts);
                foreach (var account in accounts)
                {
                    AssertDocument(await Read(raw, account.Key, transaction.Session), account.Key, "after", "2",
                                   operation.EndsWith("with") ? "changed" : "keep");
                    AssertDocument(await Read(raw, account.Key), account.Key, "before", "1", "keep");
                }
                if (mode == "commit")
                    await transaction.CommitAsync();
                else
                    await transaction.AbortAsync();
            }

            foreach (var account in accounts)
                AssertDocument(await Read(raw, account.Key), account.Key, mode == "abort" ? "before" : "after",
                               mode == "abort" ? "1" : "2", mode != "abort" && operation.EndsWith("with") ? "changed" : "keep");
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.Database().DatabaseNamespace.DatabaseName);
        }
    }

    [TestMethod]
    public async Task entity_update_does_not_change_mapped_identity_or_remove_preexisting_unmapped_data()
    {
        var db = await InitTest.InitTestDatabase("mapped_identity_" + Guid.NewGuid().ToString("N"));
        try
        {
            var account = new MappedAccount();
            await db.SaveAsync(account);
            var raw = db.Database().GetCollection<BsonDocument>(db.CollectionName<MappedAccount>());
            await raw.UpdateOneAsync(new BsonDocument("_id", account.Key), Builders<BsonDocument>.Update.Set("Secret", "already stored"));
            var originalKey = account.Key;
            account.Key = "different-id";
            account.Name = "after";
            await db.Update<MappedAccount>().MatchID(originalKey).ModifyWith(account).ExecuteAsync();
            var stored = await Read(raw, originalKey);
            Assert.AreEqual(originalKey, stored["_id"].AsString);
            Assert.AreEqual("after", stored["name"].AsString);
            Assert.AreEqual("already stored", stored["Secret"].AsString);
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.Database().DatabaseNamespace.DatabaseName);
        }
    }

    static async Task Write(DB db, string operation, MappedAccount[] accounts)
    {
        var selected = new[] { nameof(MappedAccount.Name), nameof(MappedAccount.Score), nameof(MappedAccount.Secret),
                              nameof(MappedAccount.InheritedSecret), nameof(MappedAccount.ConventionSecret), nameof(MappedAccount.IgnoredSecret) };
        switch (operation)
        {
            case "batch-only":
                await db.SaveOnlyAsync(accounts, a => new { a.Name, a.Score, a.Secret, a.InheritedSecret, a.ConventionSecret, a.IgnoredSecret });
                return;
            case "batch-only-names":
                await db.SaveOnlyAsync(accounts, selected);
                return;
            case "batch-except":
                await db.SaveExceptAsync(accounts, a => new { a.Keep });
                return;
            case "batch-except-names":
                await db.SaveExceptAsync(accounts, new[] { nameof(MappedAccount.Keep) });
                return;
        }

        foreach (var account in accounts)
        {
            switch (operation)
            {
                case "save-only":
                    await db.SaveOnlyAsync(account, a => new { a.Name, a.Score, a.Secret, a.InheritedSecret, a.ConventionSecret, a.IgnoredSecret });
                    break;
                case "save-only-names":
                    await db.SaveOnlyAsync(account, selected);
                    break;
                case "save-except":
                    await db.SaveExceptAsync(account, a => new { a.Keep });
                    break;
                case "save-except-names":
                    await db.SaveExceptAsync(account, new[] { nameof(MappedAccount.Keep) });
                    break;
                case "preserving":
                    await db.SavePreservingAsync(account);
                    break;
                case "update-with":
                    await db.Update<MappedAccount>().MatchID(account.Key).ModifyWith(account).ExecuteAsync();
                    break;
                case "update-only":
                    await db.Update<MappedAccount>().MatchID(account.Key)
                            .ModifyOnly(a => new { a.Name, a.Score, a.Secret, a.InheritedSecret, a.ConventionSecret, a.IgnoredSecret }, account).ExecuteAsync();
                    break;
                case "update-except":
                    await db.Update<MappedAccount>().MatchID(account.Key).ModifyExcept(a => new { a.Keep }, account).ExecuteAsync();
                    break;
                case "get-with":
                    Assert.IsNotNull(await db.UpdateAndGet<MappedAccount>().MatchID(account.Key).ModifyWith(account).ExecuteAsync());
                    break;
                case "get-only":
                    Assert.IsNotNull(await db.UpdateAndGet<MappedAccount>().MatchID(account.Key)
                                            .ModifyOnly(a => new { a.Name, a.Score, a.Secret, a.InheritedSecret, a.ConventionSecret, a.IgnoredSecret }, account).ExecuteAsync());
                    break;
                case "get-except":
                    Assert.IsNotNull(await db.UpdateAndGet<MappedAccount>().MatchID(account.Key).ModifyExcept(a => new { a.Keep }, account).ExecuteAsync());
                    break;
                default:
                    Assert.Fail("Unknown operation: " + operation);
                    break;
            }
        }
    }

    static Task<BsonDocument> Read(IMongoCollection<BsonDocument> raw, string key, IClientSessionHandle? session = null)
        => (session == null ? raw.Find(new BsonDocument("_id", key)) : raw.Find(session, new BsonDocument("_id", key))).SingleAsync();

    static void AssertDocument(BsonDocument stored, string key, string name, string score, string keep)
    {
        Assert.AreEqual(key, stored["_id"].AsString);
        Assert.AreEqual(name, stored["name"].AsString);
        Assert.AreEqual(score, stored["score"].AsString);
        Assert.AreEqual(keep, stored["Keep"].AsString);
        foreach (var field in new[] { "Secret", "InheritedSecret", "ConventionSecret", "IgnoredSecret", "Key", "Name", "Score" })
            Assert.IsFalse(stored.Contains(field), "Unexpected persisted field: " + field);
    }

    class IgnoreTransientConvention : ConventionBase, IClassMapConvention
    {
        public void Apply(BsonClassMap classMap) => classMap.UnmapMember(typeof(MappedAccount).GetProperty(nameof(MappedAccount.ConventionSecret))!);
    }

    public class AccountBase : IEntity
    {
        public string Key { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Name { get; set; } = "before";
        public string InheritedSecret { get; set; } = "synthetic-inherited";
    }

    public class MappedAccount : AccountBase
    {
        public int Score { get; set; } = 1;
        [Preserve] public string Keep { get; set; } = "keep";
        public string Secret { get; set; } = "synthetic-secret";
        public string ConventionSecret { get; set; } = "synthetic-convention";
        [BsonIgnore] public string IgnoredSecret { get; set; } = "synthetic-ignored";
    }
}
