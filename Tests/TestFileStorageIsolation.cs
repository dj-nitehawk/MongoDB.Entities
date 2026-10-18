using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Entities.Tests;

[TestClass]
public class FileStorageIsolationTests
{
    static FileStorageIsolationTests()
    {
        BsonClassMap.RegisterClassMap<FileEntity<RelatedFile>>(map =>
        {
            map.AutoMap();
            map.GetMemberMap(nameof(RelatedFile.BinaryStorageID)).SetElementName("storage_key");
        });
        BsonClassMap.RegisterClassMap<FileEntity<MappedFile>>(map =>
        {
            map.AutoMap();
            map.GetMemberMap(nameof(MappedFile.BinaryStorageID)).SetElementName("storage_key");
            map.GetMemberMap(nameof(MappedFile.FileSize)).SetElementName("file_size");
            map.GetMemberMap(nameof(MappedFile.ChunkCount)).SetElementName("chunk_count");
            map.GetMemberMap(nameof(MappedFile.UploadSuccessful)).SetElementName("upload_successful");
        });
        ConventionRegistry.Register("file-storage-camel-case",
                                    new ConventionPack { new CamelCaseElementNameConvention() },
                                    type => type == typeof(FileEntity<ConventionFile>) || type == typeof(ConventionFile));
    }

    public class MappedFile : FileEntity<MappedFile>;
    public class ConventionFile : FileEntity<ConventionFile>;

    [TestMethod]
    [DataRow("entity", false)]
    [DataRow("id", false)]
    [DataRow("ids", false)]
    [DataRow("predicate", false)]
    [DataRow("commit", false)]
    [DataRow("abort", false)]
    [DataRow("entity", true)]
    [DataRow("id", true)]
    [DataRow("ids", true)]
    [DataRow("predicate", true)]
    [DataRow("commit", true)]
    [DataRow("abort", true)]
    public Task mapped_file_deletion_removes_only_its_uploaded_chunks(string operation, bool convention)
        => Isolated(db => convention ? DeleteMapped<ConventionFile>(db, operation) : DeleteMapped<MappedFile>(db, operation));

    static async Task DeleteMapped<T>(DB db, string operation) where T : FileEntity<T>, new()
    {
        var file = new T();
        var other = new FirstFile();
        await db.SaveAsync(file);
        await db.SaveAsync(other);
        await Upload(file, db, 1);
        await Upload(other, db, 9);
        await Check(file, db, 1);
        var key = file.BinaryStorageID;
        Assert.AreEqual(1L, await db.Collection<FileChunk>().CountDocumentsAsync(c => c.BinaryStorageID == key));

        DeleteResult result;
        switch (operation)
        {
            case "entity": result = await db.DeleteAsync(file); break;
            case "id": result = await db.DeleteAsync<T>(file.ID); break;
            case "ids": result = await db.DeleteAsync<T>(new object[] { file.ID }); break;
            case "predicate": result = await db.DeleteAsync<T>(f => f.ID == file.ID); break;
            default:
                using (var tx = db.Transaction())
                {
                    result = await tx.DeleteAsync<T>(file.ID);
                    Assert.IsNull(await tx.Find<T>().OneAsync(file.ID));
                    Assert.AreEqual(0L, await tx.CountAsync<FileChunk>(c => c.BinaryStorageID == key));
                    if (operation == "commit") await tx.CommitAsync();
                    else await tx.AbortAsync();
                }
                break;
        }

        Assert.IsTrue(result.IsAcknowledged);
        Assert.AreEqual(1L, result.DeletedCount);
        Assert.AreEqual(operation == "abort" ? 1L : 0L, await db.Collection<FileChunk>().CountDocumentsAsync(c => c.BinaryStorageID == key));
        if (operation == "abort") await Check(file, db, 1);
        else Assert.IsNull(await db.Find<T>().OneAsync(file.ID));
        await Check(other, db, 9);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public Task mapped_full_saves_preserve_uploads_and_initialize_empty_managed_fields(bool batch, bool convention)
        => Isolated(db => convention
                              ? SaveMapped<ConventionFile>(db, batch, new[] { "binaryStorageID", "fileSize", "chunkCount", "uploadSuccessful" })
                              : SaveMapped<MappedFile>(db, batch, new[] { "storage_key", "file_size", "chunk_count", "upload_successful" }));

    static async Task SaveMapped<T>(DB db, bool batch, string[] fields) where T : FileEntity<T>, new()
    {
        var file = new T { ID = "explicit-file-id" };
        async Task Save()
        {
            if (batch) await db.SaveAsync(new[] { file });
            else await db.SaveAsync(file);
        }
        var raw = db.Database().GetCollection<BsonDocument>(db.CollectionName<T>());
        async Task AssertEmpty()
        {
            var document = await raw.Find(new BsonDocument("_id", file.ID)).SingleAsync();
            Assert.IsTrue(document[fields[0]].IsBsonNull);
            Assert.AreEqual(0L, document[fields[1]].AsInt64);
            Assert.AreEqual(0, document[fields[2]].AsInt32);
            Assert.IsFalse(document[fields[3]].AsBoolean);
            foreach (var name in new[] { "BinaryStorageID", "FileSize", "ChunkCount", "UploadSuccessful" })
                Assert.IsFalse(document.Contains(name));
            var persisted = (await db.Find<T>().OneAsync(file.ID))!;
            Assert.IsNull(persisted.BinaryStorageID);
            Assert.AreEqual(0L, persisted.FileSize);
            Assert.AreEqual(0, persisted.ChunkCount);
            Assert.IsFalse(persisted.UploadSuccessful);
        }

        await Save();
        await AssertEmpty();
        await Upload(file, db, 1);
        var key = file.BinaryStorageID;
        await Save();
        await Check(file, db, 1);
        Assert.AreEqual(key, (await db.Find<T>().OneAsync(file.ID))!.BinaryStorageID);
        await raw.DeleteOneAsync(new BsonDocument("_id", file.ID));
        await Save();
        await AssertEmpty();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task mapped_legacy_deletion_keeps_new_format_chunks(bool nullReference)
        => Isolated(async db =>
        {
            var file = new MappedFile { ID = ObjectId.GenerateNewId().ToString() };
            var metadata = new BsonDocument
            {
                { "_id", file.ID }, { "file_size", 3L }, { "chunk_count", 1 }, { "upload_successful", true }
            };
            if (nullReference) metadata["storage_key"] = BsonNull.Value;
            await db.Database().GetCollection<BsonDocument>(db.CollectionName<MappedFile>()).InsertOneAsync(metadata);
            await db.Collection<FileChunk>().InsertOneAsync(new FileChunk { FileID = file.ID, Data = new byte[] { 1, 2, 3 } });
            var other = new FirstFile { ID = file.ID };
            await db.SaveAsync(other);
            await Upload(other, db, 9);
            await Check(file, db, 1);
            Assert.AreEqual(1L, (await db.DeleteAsync(file)).DeletedCount);
            Assert.IsNull(await db.Find<MappedFile>().OneAsync(file.ID));
            Assert.AreEqual(1L, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
            await Check(other, db, 9);
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task mapped_reference_classification_preserves_legacy_ownership_checks(bool legacy)
        => Isolated(async db =>
        {
            var file = new MappedFile { ID = ObjectId.GenerateNewId().ToString() };
            if (legacy)
                await db.Database().GetCollection<BsonDocument>(db.CollectionName<MappedFile>()).InsertOneAsync(new BsonDocument
                {
                    { "_id", file.ID }, { "file_size", 3L }, { "chunk_count", 1 }, { "upload_successful", true }
                });
            else
            {
                await db.SaveAsync(file);
                await Upload(file, db, 1);
            }
            await SeedLegacy<FirstFile>(db, file.ID);
            await db.Collection<FileChunk>().InsertOneAsync(new FileChunk { FileID = file.ID, Data = new byte[] { 9, 2, 3 } });

            if (legacy)
            {
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => db.DeleteAsync(file));
                await Check(file, db, 9);
            }
            else
            {
                Assert.AreEqual(1L, (await db.DeleteAsync(file)).DeletedCount);
                Assert.IsNull(await db.Find<MappedFile>().OneAsync(file.ID));
            }
            await Check(new FirstFile { ID = file.ID }, db, 9);
            Assert.AreEqual(1L, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
        });

    public class RelatedFile : FileEntity<RelatedFile>
    {
        public Many<RelatedChild, RelatedFile> Children { get; set; } = null!;
    }

    public class RelatedChild : Entity;

    [TestMethod]
    [DataRow("entity", false)]
    [DataRow("id", false)]
    [DataRow("predicate", false)]
    [DataRow("ids", false)]
    [DataRow("ids", true)]
    [DataRow("predicate", true)]
    public Task replacement_winning_conditional_delete_preserves_relationships(string caller, bool mixed)
        => Isolated(async db =>
        {
            var survivor = new RelatedFile();
            var removed = new RelatedFile { ID = ObjectId.GenerateNewId().ToString() };
            var child = new RelatedChild();
            survivor.InitOneToMany(() => survivor.Children, db);
            removed.InitOneToMany(() => removed.Children, db);
            await db.SaveAsync(child);
            await db.SaveAsync(survivor);
            await Upload(survivor, db, 1);
            await survivor.Children.AddAsync(child);
            if (mixed)
            {
                await db.SaveAsync(removed);
                await Upload(removed, db, 3);
                await removed.Children.AddAsync(child);
            }

            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var gated = 0;
            var settings = db.Database().Client.Settings.Clone();
            settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.CommandName != "delete" || command.Command["delete"] != db.CollectionName<RelatedFile>() ||
                    Interlocked.Exchange(ref gated, 1) != 0)
                    return;
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(30)))
                    throw new TimeoutException("Conditional file delete was not released.");
            });
            var deletingDb = await DB.InitAsync(db.Database().DatabaseNamespace.DatabaseName, settings);
            var deletion = Task.Run(() => caller switch
            {
                "entity" => deletingDb.DeleteAsync(survivor),
                "id" => deletingDb.DeleteAsync<RelatedFile>(survivor.ID),
                "predicate" => deletingDb.DeleteAsync<RelatedFile>(f => f.ID == survivor.ID || (mixed && f.ID == removed.ID)),
                _ => deletingDb.DeleteAsync<RelatedFile>(mixed ? new object[] { survivor.ID, removed.ID } : new object[] { survivor.ID })
            });
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await Upload(survivor, db, 9);
            }
            finally
            {
                release.Set();
                await deletion;
            }

            Assert.AreEqual(mixed ? 1L : 0L, (await deletion).DeletedCount);
            await Check(survivor, db, 9);
            Assert.AreEqual(1L, await survivor.Children.ChildrenCountAsync());
            Assert.IsNull(await db.Find<RelatedFile>().OneAsync(removed.ID));
            Assert.AreEqual(0L, await removed.Children.ChildrenCountAsync());
            Assert.IsNotNull(await db.Find<RelatedChild>().OneAsync(child.ID));
            Assert.AreEqual(1L, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
        });

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public Task file_relationship_cascade_participates_in_transaction(bool commit)
        => Isolated(async db =>
        {
            var file = new RelatedFile();
            var child = new RelatedChild();
            file.InitOneToMany(() => file.Children, db);
            await db.SaveAsync(file);
            await db.SaveAsync(child);
            await Upload(file, db, 1);
            await file.Children.AddAsync(child);
            using (var transaction = db.Transaction())
            {
                Assert.AreEqual(1L, (await transaction.DeleteAsync(file)).DeletedCount);
                if (commit) await transaction.CommitAsync();
                else await transaction.AbortAsync();
            }
            Assert.AreEqual(commit ? 0L : 1L, await file.Children.ChildrenCountAsync());
            Assert.AreEqual(commit ? 0L : 1L, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
            if (commit) Assert.IsNull(await db.Find<RelatedFile>().OneAsync(file.ID));
            else await Check(file, db, 1);
            Assert.IsNotNull(await db.Find<RelatedChild>().OneAsync(child.ID));
        });

    public class MetadataFile : FileEntity<MetadataFile>
    {
        [BsonElement("optional"), BsonIgnoreIfNull]
        public string? Optional { get; set; }

        [BsonElement("default_text"), IgnoreDefault]
        public string? DefaultText { get; set; }

        [BsonElement("default_number"), BsonDefaultValue(7), BsonIgnoreIfDefault]
        public int DefaultNumber { get; set; } = 7;

        [BsonElement("conditional")]
        public string Conditional { get; set; } = "conditional";

        [BsonIgnore]
        public bool IncludeConditional { get; set; } = true;

        public bool ShouldSerializeConditional() => IncludeConditional;
    }

    public class ExtraElementsFile : FileEntity<ExtraElementsFile>
    {
        public string Label { get; set; } = "metadata";

        [BsonExtraElements]
        public BsonDocument ExtraElements { get; set; } = new();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task streaming_metadata_reads_project_only_storage_fields(bool transactional)
        => Isolated(async db =>
        {
            var file = new MetadataFile
            {
                Optional = "application metadata",
                DefaultText = "non-default",
                DefaultNumber = 42,
                Conditional = "included"
            };
            await db.SaveAsync(file);
            await Upload(file, db, 7);
            var commands = new List<BsonDocument>();
            var settings = db.Database().Client.Settings.Clone();
            settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.CommandName == "find" && command.Command["find"] == db.CollectionName<MetadataFile>())
                    commands.Add(command.Command.DeepClone().AsBsonDocument);
            });
            var streamingDb = await DB.InitAsync(db.Database().DatabaseNamespace.DatabaseName, settings);
            using var tx = transactional ? streamingDb.Transaction() : null;
            using var output = new MemoryStream();
            // Only the identity is available locally; storage state must come from the persisted projection.
            await new MetadataFile { ID = file.ID }.Data(streamingDb).DownloadAsync(output, session: tx?.Session);
            CollectionAssert.AreEqual(new byte[] { 7, 2, 3 }, output.ToArray());
            Assert.AreEqual(1, commands.Count);
            var projection = commands[0]["projection"].AsBsonDocument;
            Assert.AreEqual(3, projection.ElementCount);
            Assert.AreEqual(1, projection["BinaryStorageID"].AsInt32);
            Assert.AreEqual(1, projection["UploadSuccessful"].AsInt32);
            Assert.AreEqual(0, projection["_id"].AsInt32);
            Assert.AreEqual(transactional, commands[0].Contains("autocommit"));
            if (tx != null) await tx.AbortAsync();
        });

    public class HookFile : FileEntity<HookFile>, ICreatedOn, IModifiedOn
    {
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public UpdatedBy ModifiedBy { get; set; }
        public int HookCount { get; set; }
        public string? IdSeenByHook { get; set; }
        public bool CreatedOnSeenByHook { get; set; }
        public bool ModifiedOnSeenByHook { get; set; }
    }

    class HookDb : DB
    {
        public HookDb(DB db) : base(db)
        {
            ModifiedBy = new UpdatedBy { UserType = "save-hook" };
        }

        protected override Action<T> OnBeforeSave<T>()
            => entity =>
            {
                var file = (HookFile)(object)entity;
                Assert.AreEqual("save-hook", file.ModifiedBy.UserType);
                file.HookCount++;
                file.IdSeenByHook = file.ID;
                file.CreatedOnSeenByHook = file.CreatedOn != default;
                file.ModifiedOnSeenByHook = file.ModifiedOn != default;
                file.BinaryStorageID = "hook-storage";
                file.FileSize = 91;
                file.ChunkCount = 9;
                file.UploadSuccessful = true;
            };
    }

    public class FirstFile : FileEntity<FirstFile>
    {
        public string Name { get; set; } = "first";
        public string Note { get; set; } = "note";
    }

    [Collection("custom-file-collection")]
    public class SecondFile : FileEntity<SecondFile>;

    static async Task Upload<T>(T file, DB db, byte value) where T : FileEntity<T>, new()
    {
        using var input = new MemoryStream(new byte[] { value, 2, 3 });
        await file.Data(db).UploadAsync(input);
    }

    static async Task Check<T>(T file, DB db, byte value) where T : FileEntity<T>, new()
    {
        using var output = new MemoryStream();
        await file.Data(db).DownloadAsync(output);
        CollectionAssert.AreEqual(new byte[] { value, 2, 3 }, output.ToArray());
        var persisted = await db.Find<T>().OneAsync(file.ID);
        Assert.IsNotNull(persisted);
        Assert.IsTrue(persisted.UploadSuccessful);
        Assert.AreEqual(3, persisted.FileSize);
        Assert.AreEqual(1, persisted.ChunkCount);
    }

    static async Task Isolated(Func<DB, Task> action)
    {
        var db = await InitTest.InitTestDatabase("file-storage-" + Guid.NewGuid().ToString("N"));
        try { await action(db); }
        finally { await db.Database().Client.DropDatabaseAsync(db.Database().DatabaseNamespace.DatabaseName); }
    }

    [TestMethod]
    public Task uploads_retry_index_initialization_after_failure()
        => Isolated(async db =>
        {
            var first = new FirstFile();
            var second = new SecondFile();
            await db.SaveAsync(first);
            await db.SaveAsync(second);
            var chunksName = db.Collection<FileChunk>().CollectionNamespace.CollectionName;
            await db.Database().CreateCollectionAsync("index-view-source");
            await db.Database().CreateViewAsync<BsonDocument, BsonDocument>(
                chunksName, "index-view-source", new EmptyPipelineDefinition<BsonDocument>());

            await Task.WhenAll(
                Assert.ThrowsAsync<MongoCommandException>(() => Upload(first, db, 1)),
                Assert.ThrowsAsync<MongoCommandException>(() => Upload(second, db, 4)));
            Assert.IsFalse((await db.Find<FirstFile>().OneAsync(first.ID))!.UploadSuccessful);
            Assert.IsFalse((await db.Find<SecondFile>().OneAsync(second.ID))!.UploadSuccessful);

            await db.Database().DropCollectionAsync(chunksName);
            await db.Database().CreateCollectionAsync(chunksName);
            await Task.WhenAll(Upload(first, db, 1), Upload(second, db, 4));
            await Check(first, db, 1);
            await Check(second, db, 4);
            using var indexes = await db.Collection<FileChunk>().Indexes.ListAsync();
            var indexList = await indexes.ToListAsync();
            Assert.IsTrue(indexList.Exists(index => index["name"] == "FileID(Asc)"));
            Assert.IsTrue(indexList.Exists(index => index["name"] == "BinaryStorageID(Asc)"));
        });

    [TestMethod]
    [DataRow("chunks")]
    [DataRow("entity")]
    [DataRow("id")]
    [DataRow("list")]
    [DataRow("predicate")]
    [DataRow("commit")]
    [DataRow("abort")]
    public Task overlapping_ids_remain_isolated_through_replacement_reload_and_deletion(string operation)
        => Isolated(async db =>
        {
            var first = new FirstFile { ID = "shared-file-id" };
            var second = new SecondFile { ID = first.ID };
            await db.SaveAsync(first);
            await db.SaveAsync(second);
            await Upload(first, db, 1);
            await Upload(second, db, 7);
            await Upload(first, db, 4);
            await Check(second, db, 7);
            first = (await db.Find<FirstFile>().OneAsync(first.ID))!;
            second = (await db.Find<SecondFile>().OneAsync(second.ID))!;
            await Check(first, db, 4);
            Assert.AreNotEqual(first.BinaryStorageID, second.BinaryStorageID);
            var secondKey = second.BinaryStorageID;
            switch (operation)
            {
                case "chunks": await db.File<SecondFile>(second.ID).DeleteBinaryChunks(); break;
                case "entity": await db.DeleteAsync(second); break;
                case "id": await db.DeleteAsync<SecondFile>(second.ID); break;
                case "list": await db.DeleteAsync<SecondFile>(new object[] { second.ID }); break;
                case "predicate": await db.DeleteAsync<SecondFile>(f => f.ID == second.ID); break;
                default:
                    using (var tx = db.Transaction())
                    {
                        await tx.DeleteAsync<SecondFile>(second.ID);
                        if (operation == "commit") await tx.CommitAsync();
                        else await tx.AbortAsync();
                    }
                    break;
            }
            await Check(first, db, 4);
            Assert.AreEqual(operation == "abort" ? 1 : 0,
                            await db.Collection<FileChunk>().CountDocumentsAsync(c => c.BinaryStorageID == secondKey));
            if (operation == "abort") await Check(second, db, 7);
            if (operation == "chunks")
            {
                var cleared = (await db.Find<SecondFile>().OneAsync(second.ID))!;
                Assert.IsFalse(cleared.UploadSuccessful);
                Assert.AreEqual(0, cleared.FileSize);
                Assert.AreEqual(0, cleared.ChunkCount);
            }
        });

    [TestMethod]
    public Task failed_replacements_preserve_previous_bytes_and_metadata()
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var key = file.BinaryStorageID;
            file.Md5 = "wrong";
            using var input = new MemoryStream(new byte[300 * 1024]);
            await Assert.ThrowsExactlyAsync<InvalidDataException>(() => file.Data(db).UploadAsync(input));
            Assert.AreEqual(key, file.BinaryStorageID);
            await Check(file, db, 1);
            file.Md5 = null;
            using var empty = new MemoryStream();
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => file.Data(db).UploadAsync(empty));
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => file.Data(db).UploadAsync(input, cancellation: cancelled.Token));
            await Check(file, db, 1);
            Assert.AreEqual(1, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
        });

    [TestMethod]
    [DataRow("save", "null")]
    [DataRow("save", "empty")]
    [DataRow("save", "explicit")]
    [DataRow("batch-save", "null")]
    [DataRow("batch-save", "empty")]
    [DataRow("batch-save", "explicit")]
    [DataRow("batch-save", "existing")]
    [DataRow("insert", "null")]
    [DataRow("insert", "empty")]
    [DataRow("insert", "explicit")]
    [DataRow("batch-insert", "null")]
    [DataRow("batch-insert", "empty")]
    [DataRow("batch-insert", "explicit")]
    [DataRow("save-only", "null")]
    [DataRow("save-only", "empty")]
    [DataRow("save-only", "explicit")]
    [DataRow("save-only", "existing")]
    [DataRow("batch-save-only", "null")]
    [DataRow("batch-save-only", "empty")]
    [DataRow("batch-save-only", "explicit")]
    [DataRow("batch-save-only", "existing")]
    [DataRow("save-except", "null")]
    [DataRow("save-except", "empty")]
    [DataRow("save-except", "explicit")]
    [DataRow("save-except", "existing")]
    [DataRow("batch-save-except", "null")]
    [DataRow("batch-save-except", "empty")]
    [DataRow("batch-save-except", "explicit")]
    [DataRow("batch-save-except", "existing")]
    public Task save_preparation_preserves_hook_order_and_upload_ownership(string api, string idMode)
        => Isolated(async db =>
        {
            var file = new HookFile
            {
                ID = idMode switch { "null" => null!, "empty" => "", _ => "hook-file" }
            };
            string? existingStorage = null;
            if (idMode == "existing")
            {
                await db.WithModifiedBy(new UpdatedBy()).SaveAsync(file);
                await Upload(file, db, 6);
                existingStorage = file.BinaryStorageID;
            }

            var originalId = file.ID;
            var hookDb = new HookDb(db);
            var partial = api.Contains("only") || api.Contains("except");
            var included = new[]
            {
                nameof(HookFile.HookCount), nameof(HookFile.IdSeenByHook), nameof(HookFile.CreatedOnSeenByHook),
                nameof(HookFile.ModifiedOnSeenByHook), nameof(HookFile.CreatedOn), nameof(HookFile.ModifiedOn), nameof(HookFile.ModifiedBy)
            };
            switch (api)
            {
                case "save": await hookDb.SaveAsync(file); break;
                case "batch-save": await hookDb.SaveAsync(new[] { file }); break;
                case "insert": await hookDb.InsertAsync(file); break;
                case "batch-insert": await hookDb.InsertAsync(new[] { file }); break;
                case "save-only": await hookDb.SaveOnlyAsync(file, included); break;
                case "batch-save-only": await hookDb.SaveOnlyAsync(new[] { file }, included); break;
                case "save-except": await hookDb.SaveExceptAsync(file, f => new { f.BinaryStorageID }); break;
                case "batch-save-except": await hookDb.SaveExceptAsync(new[] { file }, f => new { f.BinaryStorageID }); break;
            }

            Assert.IsFalse(string.IsNullOrEmpty(file.ID));
            Assert.AreEqual(api == "save" ? originalId : file.ID, file.IdSeenByHook);
            Assert.AreEqual(1, file.HookCount);
            Assert.AreEqual(api != "save" && string.IsNullOrEmpty(originalId), file.CreatedOnSeenByHook);
            Assert.AreEqual(api != "save", file.ModifiedOnSeenByHook);
            Assert.AreNotEqual(default, file.ModifiedOn);
            if (string.IsNullOrEmpty(originalId)) Assert.AreNotEqual(default, file.CreatedOn);

            var reset = !partial && (api == "insert" || api == "batch-insert" || string.IsNullOrEmpty(originalId));
            Assert.AreEqual(reset ? null : "hook-storage", file.BinaryStorageID);
            Assert.AreEqual(reset ? 0L : 91L, file.FileSize);
            Assert.AreEqual(reset ? 0 : 9, file.ChunkCount);
            Assert.AreEqual(!reset, file.UploadSuccessful);

            var persisted = (await db.Find<HookFile>().OneAsync(file.ID))!;
            Assert.AreEqual(1, persisted.HookCount);
            Assert.AreEqual(file.IdSeenByHook, persisted.IdSeenByHook);
            Assert.AreEqual("save-hook", persisted.ModifiedBy.UserType);
            Assert.AreEqual(new BsonDateTime(file.CreatedOn).ToUniversalTime(), persisted.CreatedOn);
            Assert.AreEqual(new BsonDateTime(file.ModifiedOn).ToUniversalTime(), persisted.ModifiedOn);
            Assert.AreEqual(existingStorage, persisted.BinaryStorageID);
            Assert.AreEqual(idMode == "existing" ? 3L : 0L, persisted.FileSize);
            Assert.AreEqual(idMode == "existing" ? 1 : 0, persisted.ChunkCount);
            Assert.AreEqual(idMode == "existing", persisted.UploadSuccessful);
            if (idMode == "existing") await Check(persisted, db, 6);
        });

    [TestMethod]
    [DataRow("save", "explicit")]
    [DataRow("save", "null")]
    [DataRow("save", "empty")]
    [DataRow("batch-save", "explicit")]
    [DataRow("batch-save", "null")]
    [DataRow("batch-save", "empty")]
    [DataRow("insert", "explicit")]
    [DataRow("insert", "null")]
    [DataRow("insert", "empty")]
    [DataRow("batch-insert", "explicit")]
    [DataRow("batch-insert", "null")]
    [DataRow("batch-insert", "empty")]
    public Task copied_file_metadata_starts_empty_and_cannot_delete_source_bytes(string api, string idMode)
        => Isolated(async db =>
        {
            var source = new FirstFile { ID = "source" };
            await db.SaveAsync(source);
            await Upload(source, db, 9);

            foreach (var operation in new[] { "delete", "clear", "replace" })
            {
                var copy = (await db.Find<FirstFile>().OneAsync(source.ID))!;
                copy.ID = idMode switch
                {
                    "null" => null!,
                    "empty" => "",
                    _ => "copy-" + operation
                };
                copy.Name = "copied metadata";
                switch (api)
                {
                    case "save": await db.SaveAsync(copy); break;
                    case "batch-save": await db.SaveAsync(new[] { copy }); break;
                    case "insert": await db.InsertAsync(copy); break;
                    case "batch-insert": await db.InsertAsync(new[] { copy }); break;
                }

                Assert.AreNotEqual(source.ID, copy.ID);
                var persisted = await db.Find<FirstFile>().OneAsync(copy.ID);
                Assert.IsNotNull(persisted);
                Assert.AreEqual("copied metadata", persisted.Name);
                Assert.IsNull(persisted.BinaryStorageID);
                Assert.IsFalse(persisted.UploadSuccessful);
                Assert.AreEqual(0L, persisted.FileSize);
                Assert.AreEqual(0, persisted.ChunkCount);
                await Check(source, db, 9);

                switch (operation)
                {
                    case "delete": await db.DeleteAsync<FirstFile>(copy.ID); break;
                    case "clear": await copy.Data(db).DeleteBinaryChunks(); break;
                    case "replace":
                        await Upload(copy, db, 7);
                        await Check(copy, db, 7);
                        Assert.AreNotEqual(source.BinaryStorageID, copy.BinaryStorageID);
                        break;
                }
                await Check(source, db, 9);
                Assert.AreEqual(source.BinaryStorageID, (await db.Find<FirstFile>().OneAsync(source.ID))!.BinaryStorageID);
            }
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task full_saves_clear_omitted_metadata_without_changing_the_upload(bool batch)
        => Isolated(async db =>
        {
            var file = new MetadataFile
            {
                Md5 = "5289DF737DF57326FCDD22597AFB1FAC",
                Optional = "old",
                DefaultText = "old",
                DefaultNumber = 8
            };
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var storageID = file.BinaryStorageID;
            var raw = db.Database().GetCollection<BsonDocument>(db.Collection<MetadataFile>().CollectionNamespace.CollectionName);
            var before = await raw.Find(new BsonDocument("_id", file.ID)).SingleAsync();
            Assert.AreEqual("old", before["optional"].AsString);
            Assert.AreEqual("old", before["default_text"].AsString);
            Assert.AreEqual(8, before["default_number"].AsInt32);
            Assert.IsFalse(before.Contains(nameof(MetadataFile.IncludeConditional)));

            file.Md5 = null;
            file.Optional = null;
            file.DefaultText = null;
            file.DefaultNumber = 7;
            file.IncludeConditional = false;
            // Partial saves must keep their existing omission behavior.
            await db.SaveOnlyAsync(file, f => new { f.Md5, f.Optional, f.DefaultText, f.Conditional });
            var partial = (await db.Find<MetadataFile>().OneAsync(file.ID))!;
            Assert.AreEqual("5289DF737DF57326FCDD22597AFB1FAC", partial.Md5);
            Assert.AreEqual("old", partial.Optional);
            Assert.AreEqual("old", partial.DefaultText);

            if (batch)
                await db.SaveAsync(new[] { file });
            else
                await db.SaveAsync(file);

            var persisted = (await db.Find<MetadataFile>().OneAsync(file.ID))!;
            Assert.IsNull(persisted.Md5);
            Assert.IsNull(persisted.Optional);
            Assert.IsNull(persisted.DefaultText);
            Assert.AreEqual(7, persisted.DefaultNumber);
            Assert.AreEqual(storageID, persisted.BinaryStorageID);
            await Check(persisted, db, 1);
            var cleared = await raw.Find(new BsonDocument("_id", file.ID)).SingleAsync();
            foreach (var field in new[] { "Md5", "optional", "default_text", "default_number", "conditional" })
                Assert.IsFalse(cleared.Contains(field), field);

            await Upload(persisted, db, 4);
            await Check(persisted, db, 4);
        });

    [TestMethod]
    [DataRow(false, "none")]
    [DataRow(true, "none")]
    [DataRow(false, "commit")]
    [DataRow(true, "commit")]
    [DataRow(false, "abort")]
    [DataRow(true, "abort")]
    public Task full_saves_remove_deleted_extra_elements_and_preserve_uploads(bool batch, string transaction)
        => Isolated(async db =>
        {
            var file = new ExtraElementsFile
            {
                ExtraElements = new BsonDocument { { "retained", 1 }, { "removed", 2 } }
            };
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var stale = (await db.Find<ExtraElementsFile>().OneAsync(file.ID))!;
            await Upload(file, db, 4);
            var storageID = file.BinaryStorageID;
            stale.ExtraElements.Remove("removed");
            stale.ExtraElements["retained"] = "$UploadSuccessful";
            stale.ExtraElements["added"] = new BsonDocument("$literal", "$value");

            using var tx = transaction == "none" ? null : db.Transaction();
            var writer = tx ?? db;
            if (batch)
                await writer.SaveAsync(new[] { stale });
            else
                await writer.SaveAsync(stale);
            var saved = (await writer.Find<ExtraElementsFile>().OneAsync(file.ID))!;
            Assert.IsFalse(saved.ExtraElements.Contains("removed"));
            Assert.AreEqual("$UploadSuccessful", saved.ExtraElements["retained"].AsString);
            Assert.AreEqual(new BsonDocument("$literal", "$value"), saved.ExtraElements["added"]);
            Assert.AreEqual("metadata", saved.Label);
            Assert.AreEqual(storageID, saved.BinaryStorageID);
            Assert.IsTrue(saved.UploadSuccessful);
            Assert.AreEqual(3, saved.FileSize);
            Assert.AreEqual(1, saved.ChunkCount);
            if (tx != null)
            {
                if (transaction == "commit") await tx.CommitAsync();
                else await tx.AbortAsync();
            }

            var persisted = (await db.Find<ExtraElementsFile>().OneAsync(file.ID))!;
            Assert.AreEqual(transaction == "abort", persisted.ExtraElements.Contains("removed"));
            Assert.AreEqual(storageID, persisted.BinaryStorageID);
            await Check(persisted, db, 4);

            // Clearing the entire extra-elements document must remove its persisted keys too.
            persisted.ExtraElements.Clear();
            if (batch) await db.SaveAsync(new[] { persisted });
            else await db.SaveAsync(persisted);
            Assert.AreEqual(0, (await db.Find<ExtraElementsFile>().OneAsync(file.ID))!.ExtraElements.ElementCount);
            await Check(persisted, db, 4);
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task full_save_upserts_initialize_only_empty_upload_metadata(bool batch)
        => Isolated(async db =>
        {
            var file = new MetadataFile { Optional = "kept" };
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            await db.Collection<MetadataFile>().DeleteOneAsync(f => f.ID == file.ID);
            // A stale object's upload reference must not be copied into a new document.
            if (batch)
                await db.SaveAsync(new[] { file });
            else
                await db.SaveAsync(file);

            var persisted = (await db.Find<MetadataFile>().OneAsync(file.ID))!;
            Assert.AreEqual("kept", persisted.Optional);
            Assert.IsNull(persisted.BinaryStorageID);
            Assert.AreEqual(0, persisted.FileSize);
            Assert.AreEqual(0, persisted.ChunkCount);
            Assert.IsFalse(persisted.UploadSuccessful);
        });

    [TestMethod]
    public Task projected_full_and_partial_saves_do_not_erase_or_restore_storage_references()
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var stale = (await db.Find<FirstFile>().OneAsync(file.ID))!;
            await Upload(file, db, 7);
            var projected = await db.Find<FirstFile>().MatchID(file.ID).Project(p => p.Include(f => f.Name)).ExecuteSingleAsync();
            Assert.IsNotNull(projected);
            await db.SaveAsync(projected);
            await db.SaveAsync(new[] { stale });
            await db.SaveOnlyAsync(stale, f => new { f.Name, f.BinaryStorageID });
            await db.SaveExceptAsync(new[] { stale }, f => new { f.Name });
            await Check(projected, db, 7);
            Assert.AreEqual(file.BinaryStorageID, (await db.Find<FirstFile>().OneAsync(file.ID))!.BinaryStorageID);
        });

    static async Task SeedLegacy<T>(DB db, string id) where T : FileEntity<T>, new()
    {
        await db.Database().GetCollection<BsonDocument>(db.CollectionName<T>()).InsertOneAsync(new BsonDocument
        {
            { "_id", id }, { "FileSize", 3L }, { "ChunkCount", 1 }, { "UploadSuccessful", true }
        });
    }

[TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task multi_legacy_preflight_inventories_once_before_any_deletes(bool conflict)
        => Isolated(async db =>
        {
            var first = new RelatedFile { ID = ObjectId.GenerateNewId().ToString() };
            var second = new RelatedFile { ID = "later-legacy-file" };
            var child = new RelatedChild();
            first.InitOneToMany(() => first.Children, db);
            second.InitOneToMany(() => second.Children, db);
            await db.SaveAsync(child);
            foreach (var file in new[] { first, second })
            {
                await SeedLegacy<RelatedFile>(db, file.ID);
                await db.Collection<FileChunk>().InsertOneAsync(new FileChunk
                {
                    ID = ObjectId.GenerateNewId().ToString(), FileID = file.ID, Data = new byte[] { 1, 2, 3 }
                });
                await file.Children.AddAsync(child);
            }
            await SeedLegacy<SecondFile>(db, conflict ? second.ID : "unrelated-legacy-file");

            Task<List<BsonDocument>> Snapshot<T>() where T : IEntity
                => db.Database().GetCollection<BsonDocument>(db.CollectionName<T>())
                     .Find(new BsonDocument()).Sort(new BsonDocument("_id", 1)).ToListAsync();

            var filesBefore = await Snapshot<RelatedFile>();
            var otherFilesBefore = await Snapshot<SecondFile>();
            var chunksBefore = await Snapshot<FileChunk>();
            var inventories = 0;
            var writes = 0;
            var checkedIDs = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var settings = db.Database().Client.Settings.Clone();
            settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.CommandName == "listCollections")
                    Interlocked.Increment(ref inventories);
                if (command.CommandName is "insert" or "update" or "delete" or "findAndModify" or "bulkWrite")
                    Interlocked.Increment(ref writes);
                if (command.CommandName == "find" && command.Command["find"] == db.CollectionName<SecondFile>())
                    checkedIDs.Enqueue(command.Command["filter"]["_id"]["$in"][0].AsString);
            });
            var deletingDb = await DB.InitAsync(db.Database().DatabaseNamespace.DatabaseName, settings);
            Task<DeleteResult> Delete() => deletingDb.DeleteAsync<RelatedFile>(new object[] { first.ID, second.ID });

            if (conflict)
            {
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(Delete);
                Assert.AreEqual(0, writes);
                CollectionAssert.AreEqual(filesBefore, await Snapshot<RelatedFile>());
                CollectionAssert.AreEqual(chunksBefore, await Snapshot<FileChunk>());
                await Check(first, db, 1);
                await Check(second, db, 1);
                Assert.AreEqual(1L, await first.Children.ChildrenCountAsync());
                Assert.AreEqual(1L, await second.Children.ChildrenCountAsync());
            }
            else
            {
                Assert.AreEqual(2L, (await Delete()).DeletedCount);
                Assert.AreEqual(0, (await Snapshot<RelatedFile>()).Count);
                Assert.AreEqual(0, (await Snapshot<FileChunk>()).Count);
                Assert.AreEqual(0L, await first.Children.ChildrenCountAsync());
                Assert.AreEqual(0L, await second.Children.ChildrenCountAsync());
            }

            Assert.AreEqual(1, inventories);
            CollectionAssert.AreEqual(new[] { first.ID, second.ID }, checkedIDs.ToArray());
            CollectionAssert.AreEqual(otherFilesBefore, await Snapshot<SecondFile>());
            Assert.IsNotNull(await db.Find<RelatedChild>().OneAsync(child.ID));
        });

    [TestMethod]
    public Task new_format_deletion_does_not_inventory_collections()
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var inventories = 0;
            var settings = db.Database().Client.Settings.Clone();
            settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.CommandName == "listCollections")
                    Interlocked.Increment(ref inventories);
            });
            var deletingDb = await DB.InitAsync(db.Database().DatabaseNamespace.DatabaseName, settings);

            Assert.AreEqual(1L, (await deletingDb.DeleteAsync<FirstFile>(file.ID)).DeletedCount);
            Assert.AreEqual(0, inventories);
            Assert.AreEqual(0L, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
        });

    [TestMethod]
    public Task batch_legacy_conflict_rejects_all_deletes_before_any_writes()
        => Isolated(async db =>
        {
            var uploaded = new RelatedFile { ID = "a-deletable-file" };
            var legacy = new RelatedFile { ID = "b-conflicting-file" };
            var child = new RelatedChild();
            uploaded.InitOneToMany(() => uploaded.Children, db);
            legacy.InitOneToMany(() => legacy.Children, db);
            await db.SaveAsync(child);
            await db.SaveAsync(uploaded);
            await Upload(uploaded, db, 7);
            await SeedLegacy<RelatedFile>(db, legacy.ID);
            await SeedLegacy<SecondFile>(db, legacy.ID);
            await db.Collection<FileChunk>().InsertOneAsync(new FileChunk
            {
                ID = ObjectId.GenerateNewId().ToString(), FileID = legacy.ID, Data = new byte[] { 1, 2, 3 }
            });
            await uploaded.Children.AddAsync(child);
            await legacy.Children.AddAsync(child);

            Task<List<BsonDocument>> Snapshot<T>() where T : IEntity
                => db.Database().GetCollection<BsonDocument>(db.CollectionName<T>())
                     .Find(new BsonDocument()).Sort(new BsonDocument("_id", 1)).ToListAsync();

            var filesBefore = await Snapshot<RelatedFile>();
            var otherFilesBefore = await Snapshot<SecondFile>();
            var chunksBefore = await Snapshot<FileChunk>();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => db.DeleteAsync<RelatedFile>(new object[] { uploaded.ID, legacy.ID }));

            CollectionAssert.AreEqual(filesBefore, await Snapshot<RelatedFile>());
            CollectionAssert.AreEqual(otherFilesBefore, await Snapshot<SecondFile>());
            CollectionAssert.AreEqual(chunksBefore, await Snapshot<FileChunk>());
            await Check(uploaded, db, 7);
            await Check(legacy, db, 1);
            await Check(new SecondFile { ID = legacy.ID }, db, 1);
            Assert.AreEqual(1L, await uploaded.Children.ChildrenCountAsync());
            Assert.AreEqual(1L, await legacy.Children.ChildrenCountAsync());
            Assert.IsNotNull(await db.Find<RelatedChild>().OneAsync(child.ID));
        });

    [TestMethod]
    public Task legacy_namespace_and_ambiguous_cleanup_are_safe()
        => Isolated(async db =>
        {
            var first = new FirstFile { ID = "legacy-overlap" };
            await SeedLegacy<FirstFile>(db, first.ID);
            await SeedLegacy<SecondFile>(db, first.ID);
            await db.Collection<FileChunk>().InsertOneAsync(new FileChunk
            {
                ID = ObjectId.GenerateNewId().ToString(), FileID = first.ID, Data = new byte[] { 1, 2, 3 }
            });
            await Check(first, db, 1);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => first.Data(db).DeleteBinaryChunks());
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => db.DeleteAsync<FirstFile>(first.ID));
            await Check(new SecondFile { ID = first.ID }, db, 1);
            await Upload(first, db, 7);
            await Check(new SecondFile { ID = first.ID }, db, 1);
            Assert.AreEqual(1, await db.Collection<FileChunk>().CountDocumentsAsync(c => c.FileID == first.ID));

            // A legacy public ID equal to a new storage key must not read or delete new chunks.
            var legacyID = first.BinaryStorageID!;
            await SeedLegacy<SecondFile>(db, legacyID);
            await db.Collection<FileChunk>().InsertOneAsync(new FileChunk
            {
                ID = ObjectId.GenerateNewId().ToString(), FileID = legacyID, Data = new byte[] { 9, 2, 3 }
            });
            await Check(new SecondFile { ID = legacyID }, db, 9);
            await db.DeleteAsync<SecondFile>(legacyID);
            await Check(first, db, 7);
        });

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public Task transactional_upload_switches_reference_only_on_commit(bool commit)
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            using (var tx = db.Transaction())
            {
                using var input = new MemoryStream(new byte[] { 7, 2, 3 });
                await file.Data(db).UploadAsync(input, session: tx.Session);
                using var output = new MemoryStream();
                await file.Data(db).DownloadAsync(output, session: tx.Session);
                Assert.AreEqual(7, output.ToArray()[0]);
                await Check(new FirstFile { ID = file.ID }, db, 1);
                if (commit) await tx.CommitAsync();
                else await tx.AbortAsync();
            }
            // Even an object modified by an aborted transaction resolves persisted metadata.
            await Check(file, db, commit ? (byte)7 : (byte)1);
        });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task explicit_chunk_deletion_rejects_cancellation_without_a_session(bool cancelled)
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var key = file.BinaryStorageID;
            using var cancellation = new CancellationTokenSource();
            if (cancelled) cancellation.Cancel();

            await Assert.ThrowsExactlyAsync<NotSupportedException>(
                () => file.Data(db).DeleteBinaryChunks(cancellation: cancellation.Token));

            var persisted = (await db.Find<FirstFile>().OneAsync(file.ID))!;
            Assert.AreEqual(key, persisted.BinaryStorageID);
            await Check(file, db, 1);
            Assert.AreEqual(1, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
        });

    [TestMethod]
    public Task explicit_chunk_deletion_with_cancellation_rolls_back_on_abort()
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            var key = file.BinaryStorageID;
            using var cancellation = new CancellationTokenSource();
            using (var tx = db.Transaction())
            {
                await file.Data(db).DeleteBinaryChunks(tx.Session, cancellation.Token);
                var cleared = (await tx.Find<FirstFile>().OneAsync(file.ID))!;
                Assert.AreNotEqual(key, cleared.BinaryStorageID);
                Assert.IsFalse(cleared.UploadSuccessful);
                Assert.AreEqual(0, cleared.FileSize);
                Assert.AreEqual(0, cleared.ChunkCount);
                Assert.AreEqual(0, await db.Collection<FileChunk>().CountDocumentsAsync(
                                    tx.Session, c => c.BinaryStorageID == key));
                await tx.AbortAsync();
            }

            var persisted = (await db.Find<FirstFile>().OneAsync(file.ID))!;
            Assert.AreEqual(key, persisted.BinaryStorageID);
            await Check(file, db, 1);
            Assert.AreEqual(1, await db.Collection<FileChunk>().CountDocumentsAsync(c => c.BinaryStorageID == key));
        });

    sealed class GatedStream(byte value) : MemoryStream(new byte[] { value, 2, 3 })
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }

    static async Task ExpectGatedUploadConflict(Func<GatedStream, Task> startUpload, Func<Task> competingAction, TimeSpan? entryTimeout = null)
    {
        using var input = new GatedStream(4);
        var upload = startUpload(input);
        try
        {
            await input.Entered.Task.WaitAsync(entryTimeout ?? TimeSpan.FromSeconds(10));
            await competingAction();
        }
        catch
        {
            input.Release.TrySetResult();
            try { await upload; }
            catch { /* Observe upload failure without replacing the gate or competing-action failure. */ }
            throw;
        }
        finally { input.Release.TrySetResult(); }

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => upload);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task gated_upload_entry_timeout_releases_and_observes_upload(bool uploadFails)
    {
        var completed = false;
        var competed = false;
        async Task UploadWithoutEntering(GatedStream input)
        {
            await input.Release.Task;
            Assert.IsTrue(input.CanRead);
            completed = true;
            if (uploadFails) throw new InvalidOperationException("upload failed during cleanup");
        }

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => ExpectGatedUploadConflict(
            UploadWithoutEntering,
            () => { competed = true; return Task.CompletedTask; },
            TimeSpan.Zero));

        Assert.IsTrue(completed);
        Assert.IsFalse(competed);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task gated_upload_competing_failure_releases_and_observes_upload(bool uploadFails)
    {
        var completed = false;
        var failure = new ApplicationException("competing action failed");
        async Task Upload(GatedStream input)
        {
            Assert.AreEqual(3, await input.ReadAsync(new byte[3], 0, 3, CancellationToken.None));
            completed = true;
            if (uploadFails) throw new InvalidOperationException("upload failed during cleanup");
        }

        var actual = await Assert.ThrowsExactlyAsync<ApplicationException>(() => ExpectGatedUploadConflict(
            Upload,
            () => Task.FromException(failure)));

        Assert.AreSame(failure, actual);
        Assert.IsTrue(completed);
    }

    [TestMethod]
    public Task concurrent_replacement_cannot_remove_winning_upload()
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            await ExpectGatedUploadConflict(
                input => new FirstFile { ID = file.ID }.Data(db).UploadAsync(input),
                () => Upload(file, db, 7));
            await Check(file, db, 7);
            Assert.AreEqual(1, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
        });

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public Task deletion_prevents_in_flight_upload_from_restoring_binary_data(bool chunksOnly)
        => Isolated(async db =>
        {
            var file = new FirstFile();
            await db.SaveAsync(file);
            await Upload(file, db, 1);
            await ExpectGatedUploadConflict(
                input => file.Data(db).UploadAsync(input),
                async () =>
                {
                    if (chunksOnly) await file.Data(db).DeleteBinaryChunks();
                    else await db.DeleteAsync(file);
                });
            Assert.AreEqual(0, await db.Collection<FileChunk>().CountDocumentsAsync(FilterDefinition<FileChunk>.Empty));
            var persisted = await db.Find<FirstFile>().OneAsync(file.ID);
            if (chunksOnly) Assert.IsFalse(persisted!.UploadSuccessful);
            else Assert.IsNull(persisted);
        });

    [TestMethod]
    public Task binary_reference_resolution_stays_in_the_requested_database()
        => Isolated(db => Isolated(async other =>
        {
            var file = new FirstFile { ID = "same-id" };
            await db.SaveAsync(file);
            await other.SaveAsync(new FirstFile { ID = file.ID });
            await Upload(file, db, 1);
            await Upload(file, other, 7);
            await Check(file, db, 1);
            await Check(file, other, 7);
            await db.DeleteAsync<FirstFile>(file.ID);
            await Check(file, other, 7);
        }));
}
