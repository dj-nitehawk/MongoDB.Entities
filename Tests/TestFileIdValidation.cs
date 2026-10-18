using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class FileIdValidationTests
{
    public class GuardedFile : FileEntity<GuardedFile>;

    [TestMethod]
    [DataRow("upload", false)]
    [DataRow("download", false)]
    [DataRow("chunks", false)]
    [DataRow("id", false)]
    [DataRow("entity", false)]
    [DataRow("list", false)]
    [DataRow("predicate", false)]
    [DataRow("commit", false)]
    [DataRow("abort", false)]
    [DataRow("upload", true)]
    [DataRow("download", true)]
    [DataRow("chunks", true)]
    [DataRow("id", true)]
    [DataRow("entity", true)]
    [DataRow("list", true)]
    [DataRow("predicate", true)]
    [DataRow("commit", true)]
    [DataRow("abort", true)]
    public async Task noncanonical_hex_file_ids_are_rejected_before_side_effects(string operation, bool mixedCase)
    {
        var db = await InitTest.InitTestDatabase("file-id-guard-" + Guid.NewGuid().ToString("N"));
        try
        {
            var lower = new GuardedFile { ID = "abcdefabcdefabcdefabcdef" };
            var alias = new GuardedFile
            {
                ID = mixedCase ? "Abcdefabcdefabcdefabcdef" : lower.ID.ToUpperInvariant(),
                UploadSuccessful = true, FileSize = 3, ChunkCount = 1
            };
            await db.SaveAsync(lower);
            await db.SaveAsync(alias);
            using var input = new MemoryStream(new byte[] { 1, 2, 3 });
            await lower.Data(db).UploadAsync(input);
            using var replacement = new MemoryStream(new byte[] { 7, 8, 9 });
            using var output = new MemoryStream();
            using var tx = operation is "commit" or "abort" ? db.Transaction() : null;

            Func<Task> action = operation switch
            {
                "upload" => () => alias.Data(db).UploadAsync(replacement),
                "download" => () => db.File<GuardedFile>(alias.ID).DownloadAsync(output),
                "chunks" => () => db.File<GuardedFile>(alias.ID).DeleteBinaryChunks(),
                "id" => () => db.DeleteAsync<GuardedFile>(alias.ID),
                "entity" => () => db.DeleteAsync(alias),
                "list" => () => db.DeleteAsync<GuardedFile>(new[] { lower.ID, alias.ID }),
                "predicate" => () => db.DeleteAsync<GuardedFile>(f => f.ID == lower.ID || f.ID == alias.ID),
                _ => () => tx!.DeleteAsync<GuardedFile>(alias.ID)
            };
            var error = await Assert.ThrowsExactlyAsync<ArgumentException>(action);
            StringAssert.Contains(error.Message, "lowercase");
            if (operation == "commit")
                await tx!.CommitAsync();
            else if (operation == "abort")
                await tx!.AbortAsync();
            Assert.AreEqual(0L, replacement.Position);
            Assert.AreEqual(0L, output.Length);
            Assert.IsTrue(alias.UploadSuccessful);
            Assert.AreEqual(3L, alias.FileSize);
            Assert.AreEqual(1, alias.ChunkCount);
            Assert.IsNotNull(await db.Find<GuardedFile>().OneAsync(alias.ID));
            var survivor = await db.Find<GuardedFile>().OneAsync(lower.ID);
            Assert.IsNotNull(survivor);
            using var actual = new MemoryStream();
            await survivor.Data(db).DownloadAsync(actual);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, actual.ToArray());
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.Database().DatabaseNamespace.DatabaseName);
        }
    }

    [TestMethod]
    [DataRow("generated")]
    [DataRow("abcdefabcdefabcdefabcdef")]
    [DataRow("CUSTOM-file-id")]
    [DataRow("ABCDEF-not-an-objectid")]
    public async Task supported_file_ids_keep_existing_chunk_behavior(string id)
    {
        var db = await InitTest.InitTestDatabase("file-id-guard-" + Guid.NewGuid().ToString("N"));
        try
        {
            var file = new GuardedFile { ID = id == "generated" ? null! : id };
            await db.SaveAsync(file);
            using var input = new MemoryStream(new byte[] { 1, 2, 3 });
            await file.Data(db).UploadAsync(input);
            using var output = new MemoryStream();
            await file.Data(db).DownloadAsync(output);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, output.ToArray());
            await file.Data(db).DeleteBinaryChunks();
            await file.Data(db).UploadAsync(input);
            using (var tx = db.Transaction())
            {
                await tx.DeleteAsync<GuardedFile>(file.ID);
                await tx.AbortAsync();
            }
            var reloaded = await db.Find<GuardedFile>().OneAsync(file.ID);
            Assert.IsNotNull(reloaded);
            using var restored = new MemoryStream();
            await reloaded.Data(db).DownloadAsync(restored);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, restored.ToArray());
            await db.DeleteAsync<GuardedFile>(f => f.ID == file.ID);
            Assert.IsNull(await db.Find<GuardedFile>().OneAsync(file.ID));
        }
        finally
        {
            await db.Database().Client.DropDatabaseAsync(db.Database().DatabaseNamespace.DatabaseName);
        }
    }
}
