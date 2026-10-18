using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.Entities;

interface IFileEntity
{
    void ResetUpload();
}

static class FileStorage
{
    static readonly ConcurrentDictionary<(IMongoClient Client, string Namespace), Lazy<Task>> _indexes = new();

    internal static async Task EnsureIndexesAsync(IMongoCollection<FileChunk> collection)
    {
        var key = (collection.Database.Client, collection.CollectionNamespace.FullName);
        var initialization = _indexes.GetOrAdd(
            key,
            _ => new(
                () => collection.Indexes.CreateManyAsync(
                [
                    new(Builders<FileChunk>.IndexKeys.Ascending(c => c.FileID), new() { Name = "FileID(Asc)" }),
                    new(Builders<FileChunk>.IndexKeys.Ascending(c => c.BinaryStorageID), new() { Name = "BinaryStorageID(Asc)" })
                ])));
        try
        {
            await initialization.Value.ConfigureAwait(false);
        }
        catch
        {
            // Remove only this failed attempt, not a retry installed by another caller.
            ((ICollection<KeyValuePair<(IMongoClient, string), Lazy<Task>>>)_indexes).Remove(new(key, initialization));
            throw;
        }
    }

    internal static FilterDefinition<FileChunk> ChunkFilter(string fileID, string? storageID)
        => storageID == null
               ? Builders<FileChunk>.Filter.Eq(c => c.FileID, fileID) & Builders<FileChunk>.Filter.Eq(c => c.BinaryStorageID, null)
               : Builders<FileChunk>.Filter.Eq(c => c.BinaryStorageID, storageID);

    internal static Task DeleteChunksAsync(IMongoCollection<FileChunk> chunks,
                                           string fileID,
                                           string? storageID,
                                           IClientSessionHandle? session = null,
                                           CancellationToken cancellation = default)
    {
        var filter = ChunkFilter(fileID, storageID);
        return session == null
                   ? chunks.DeleteManyAsync(filter, cancellation)
                   : chunks.DeleteManyAsync(session, filter, null, cancellation);
    }

    internal static async Task<BsonValue[]> DeleteAsync<T>(DB db,
                                                           FilterDefinition<T> filter,
                                                           IClientSessionHandle? session,
                                                           CancellationToken cancellation) where T : IEntity
    {
        var deleted = new List<BsonValue>();
        var storageBsonName = FileMetadata.ManagedMemberName(Cache<T>.BsonClassMap, "BinaryStorageID");
        var files = await new Find<T, BsonDocument>(session, null, db)
                          .Match(filter)
                          .Project(p => p.Include(Cache<T>.IdPropName).Include("BinaryStorageID"))
                          .ExecuteAsync(cancellation).ConfigureAwait(false);

        // Complete legacy ownership checks for the whole batch before starting any deletes.
        var legacyIDs = files.Where(file => file.GetValue(storageBsonName, BsonNull.Value).IsBsonNull)
                             .Select(file => file[Cache<T>.IdBsonName].ToString()!).ToArray();
        await EnsureLegacyOwnershipAsync(db, db.CollectionName<T>(), legacyIDs, session, cancellation).ConfigureAwait(false);

        foreach (var file in files)
        {
            var storage = file.GetValue(storageBsonName, BsonNull.Value);
            var id = file[Cache<T>.IdBsonName];
            var conditional = filter & new BsonDocument(Cache<T>.IdBsonName, id) & new BsonDocument(storageBsonName, storage);
            var result = session == null
                             ? await db.Collection<T>().DeleteOneAsync(conditional).ConfigureAwait(false)
                             : await db.Collection<T>().DeleteOneAsync(session, conditional, cancellationToken: cancellation).ConfigureAwait(false);

            if (result.DeletedCount == 0)
                continue;

            deleted.Add(id);
            await DeleteChunksAsync(db.Collection<FileChunk>(), id.ToString()!, storage.IsBsonNull ? null : storage.AsString,
                                    session, session == null ? default : cancellation).ConfigureAwait(false);
        }

        return deleted.ToArray();
    }

    // There is no complete type registry. Inspect actual collections, including types not loaded by this process.
    // Fail closed if the inventory cannot be read. Global filters must not hide another legacy owner.
    internal static async Task EnsureLegacyOwnershipAsync(DB db,
                                                          string collectionName,
                                                          IReadOnlyCollection<string> fileIDs,
                                                          IClientSessionHandle? session,
                                                          CancellationToken cancellation)
    {
        if (fileIDs.Count == 0)
            return;

        var database = db.Database();
        using var names = await database.ListCollectionNamesAsync(cancellationToken: cancellation).ConfigureAwait(false);
        var collections = (await names.ToListAsync(cancellation).ConfigureAwait(false))
                          .Where(name => name != collectionName && name != db.CollectionName<FileChunk>() &&
                                         !name.StartsWith("system.", StringComparison.Ordinal)).ToArray();

        foreach (var fileID in fileIDs)
        {
            var idValues = new BsonArray { fileID };
            if (ObjectId.TryParse(fileID, out var objectID))
                idValues.Add(objectID);
            var filter = new BsonDocument
            {
                { "_id", new BsonDocument("$in", idValues) },
                { "BinaryStorageID", BsonNull.Value },
                { "FileSize", new BsonDocument("$exists", true) },
                { "ChunkCount", new BsonDocument("$exists", true) },
                { "UploadSuccessful", new BsonDocument("$exists", true) }
            };

            foreach (var name in collections)
            {
                var collection = database.GetCollection<BsonDocument>(name);
                using var matches = session == null
                                        ? await collection.FindAsync(filter, cancellationToken: cancellation).ConfigureAwait(false)
                                        : await collection.FindAsync(session, filter, cancellationToken: cancellation).ConfigureAwait(false);

                if (await matches.AnyAsync(cancellation).ConfigureAwait(false))
                    throw new InvalidOperationException("Legacy binary chunks may belong to another file collection. Reconcile the overlapping file IDs before deleting.");
            }
        }
    }
}