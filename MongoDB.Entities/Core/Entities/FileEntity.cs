using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

[assembly: InternalsVisibleTo("MongoDB.Entities.Tests")]

namespace MongoDB.Entities;

/// <summary>
/// Inherit this base class in order to create your own File Entities
/// </summary>
public abstract class FileEntity<T> : Entity, IFileEntity where T : FileEntity<T>, new()
{
    /// <summary>The library-managed reference to this file's binary upload.</summary>
    [BsonElement, BsonIgnoreIfNull]
    public string? BinaryStorageID { get; internal set; }

    /// <summary>
    /// The total size in bytes of the completed upload
    /// </summary>
    [BsonElement]
    public long FileSize { get; internal set; }

    /// <summary>
    /// The number of chunks in the completed upload
    /// </summary>
    [BsonElement]
    public int ChunkCount { get; internal set; }

    /// <summary>
    /// Returns true only when all the chunks have been stored successfully in mongodb
    /// </summary>
    [BsonElement]
    public bool UploadSuccessful { get; internal set; }

    /// <summary>
    /// If this value is set, the uploaded data will be hashed and matched against this value. If the hash is not equal, an exception will be thrown by the
    /// UploadAsync() method.
    /// </summary>
    [IgnoreDefault]
    public string? Md5 { get; set; }

    void IFileEntity.ResetUpload()
    {
        BinaryStorageID = null;
        FileSize = 0;
        ChunkCount = 0;
        UploadSuccessful = false;
    }

    /// <summary>
    /// Access the DataStreamer class for uploading and downloading data
    /// </summary>
    /// <param name="database">The database instance to use for this operation</param>
    public DataStreamer<T> Data(DB database)
        => new(this, database);
}

[Collection("[BINARY_CHUNKS]")]
sealed class FileChunk : IEntity
{
    [BsonId, ObjectId]
    public string ID { get; set; } = null!;

    [AsObjectId, BsonIgnoreIfNull]
    public string? FileID { get; set; }

    internal static void ValidateFileID(string? fileID)
    {
        // ObjectId conversion discards hex letter case, unlike string metadata IDs.
        if (ObjectId.TryParse(fileID, out var objectId) &&
            !string.Equals(fileID, objectId.ToString(), StringComparison.Ordinal))
            throw new ArgumentException("ObjectId-format file IDs must use lowercase hexadecimal characters to avoid sharing binary chunks with another file.", nameof(fileID));
    }

    [BsonIgnoreIfNull]
    public string? BinaryStorageID { get; set; }

    public byte[] Data { get; set; } = [];
}

/// <summary>
/// Provides the interface for uploading and downloading data chunks for file entities.
/// </summary>
public class DataStreamer<T> where T : FileEntity<T>, new()
{
    readonly FileEntity<T> _parent;
    readonly DB _db;
    readonly IMongoCollection<FileChunk> _chunkCollection;

    internal DataStreamer(FileEntity<T> parent, DB db)
    {
        _parent = parent;
        _db = db;
        _chunkCollection = db.Collection<FileChunk>();
    }

    /// <summary>
    /// Download binary data for this file entity from mongodb in chunks into a given stream with a timeout period.
    /// </summary>
    /// <param name="stream">The output stream to write the data</param>
    /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>
    /// <param name="batchSize"></param>
    /// <param name="session"></param>
    public Task DownloadWithTimeoutAsync(Stream stream, int timeOutSeconds, int batchSize = 1, IClientSessionHandle? session = null)
        => DownloadAsync(stream, batchSize, new CancellationTokenSource(timeOutSeconds * 1000).Token, session);

    /// <summary>
    /// Download binary data for this file entity from mongodb in chunks into a given stream.
    /// </summary>
    /// <param name="stream">The output stream to write the data</param>
    /// <param name="batchSize">The number of chunks you want returned at once</param>
    /// <param name="cancellation">An optional cancellation token.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    public async Task DownloadAsync(Stream stream, int batchSize = 1, CancellationToken cancellation = default, IClientSessionHandle? session = null)
    {
        _parent.ThrowIfUnsaved();
        FileChunk.ValidateFileID(_parent.ID);

        if (!stream.CanWrite)
            throw new NotSupportedException("The supplied stream is not writable!");

        var metadata = await ReadMetadataAsync(session, cancellation).ConfigureAwait(false);
        if (!metadata.UploadSuccessful)
            throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");
        var filter = FileStorage.ChunkFilter(_parent.ID, metadata.BinaryStorageID);
        var options = new FindOptions<FileChunk, byte[]>
        {
            BatchSize = batchSize,
            Sort = Builders<FileChunk>.Sort.Ascending(c => c.ID),
            Projection = Builders<FileChunk>.Projection.Expression(c => c.Data)
        };

        var findTask =
            session == null
                ? _chunkCollection.FindAsync(filter, options, cancellation)
                : _chunkCollection.FindAsync(session, filter, options, cancellation);

        using var cursor = await findTask.ConfigureAwait(false);
        var hasChunks = false;

        while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
        {
            foreach (var chunk in cursor.Current)
            {
                await stream.WriteAsync(chunk, 0, chunk.Length, cancellation).ConfigureAwait(false);
                hasChunks = true;
            }
        }

        if (!hasChunks)
            throw new InvalidOperationException($"No data was found for file entity with ID: {_parent.ID}");
    }

    /// <summary>
    /// Upload binary data for this file entity into mongodb in chunks from a given stream with a timeout period.
    /// </summary>
    /// <param name="stream">The input stream to read the data from</param>
    /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>
    /// <param name="chunkSizeKb">The 'average' size of one chunk in KiloBytes</param>
    /// <param name="session">An optional session if using within a transaction</param>
    public Task UploadWithTimeoutAsync(Stream stream, int timeOutSeconds, int chunkSizeKb = 256, IClientSessionHandle? session = null)
        => UploadAsync(stream, chunkSizeKb, new CancellationTokenSource(timeOutSeconds * 1000).Token, session);

    /// <summary>
    /// Upload binary data for this file entity into mongodb in chunks from a given stream.
    /// <para>TIP: Make sure to save the entity before calling this method.</para>
    /// </summary>
    /// <param name="stream">The input stream to read the data from</param>
    /// <param name="chunkSizeKb">The 'average' size of one chunk in KiloBytes</param>
    /// <param name="cancellation">An optional cancellation token.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    public async Task UploadAsync(Stream stream, int chunkSizeKb = 256, CancellationToken cancellation = default, IClientSessionHandle? session = null)
    {
        _parent.ThrowIfUnsaved();
        FileChunk.ValidateFileID(_parent.ID);
        if (chunkSizeKb is < 128 or > 4096)
            throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
        if (!stream.CanRead)
            throw new NotSupportedException("The supplied stream is not readable!");

        await FileStorage.EnsureIndexesAsync(_chunkCollection).ConfigureAwait(false);
        var previous = await ReadMetadataAsync(session, cancellation).ConfigureAwait(false);
        var storageID = Guid.NewGuid().ToString("N");
        var info = new StreamInfo(new() { BinaryStorageID = storageID },
                                  chunkSizeKb * 1024, 0, new byte[64 * 1024], new(chunkSizeKb * 1024));
        if (!string.IsNullOrEmpty(_parent.Md5))
            info.Md5 = MD5.Create();
        var published = false;
        try
        {
            if (stream is { CanSeek: true, Position: > 0 })
                stream.Position = 0;
            while ((info.ReadCount = await stream.ReadAsync(info.Buffer, 0, info.Buffer.Length, cancellation).ConfigureAwait(false)) > 0)
            {
                info.Md5?.TransformBlock(info.Buffer, 0, info.ReadCount, null, 0);
                await FlushToDbAsync(session, info, false, cancellation).ConfigureAwait(false);
            }
            if (info.FileSize == 0)
                throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
            info.Md5?.TransformFinalBlock(info.Buffer, 0, 0);
            if (info.Md5 != null && !BitConverter.ToString(info.Md5.Hash).Replace("-", "").Equals(_parent.Md5, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("MD5 of uploaded data doesn't match with file entity MD5.");
            await FlushToDbAsync(session, info, true, cancellation).ConfigureAwait(false);
            // A transport failure can leave the outcome unknown. Never delete a possibly published upload.
            published = true;
            if (!await SwitchMetadataAsync(previous.BinaryStorageID, storageID, info.FileSize, info.ChunkCount, true, session, cancellation).ConfigureAwait(false))
            {
                published = false;
                throw new InvalidOperationException("The file was deleted or its binary data changed concurrently. Reload and retry.");
            }
            // Legacy chunks may have other owners. Retain them when moving to isolated storage.
            if (previous.BinaryStorageID != null)
                await DeleteChunksAsync(previous.BinaryStorageID, session).ConfigureAwait(false);
        }
        catch
        {
            if (!published)
                await DeleteChunksAsync(storageID, session).ConfigureAwait(false);
            throw;
        }
        finally
        {
            info.Md5?.Dispose();
        }
    }

    /// <summary>
    /// Deletes only the binary chunks stored in the database for this file entity.
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token.</param>
    public Task DeleteBinaryChunks(IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        _parent.ThrowIfUnsaved();
        FileChunk.ValidateFileID(_parent.ID);

        return cancellation != CancellationToken.None && session == null
                   ? throw new NotSupportedException("Cancellation is only supported within transactions for deleting binary chunks!")
                   : CleanUpAsync(session, cancellation);
    }

    async Task CleanUpAsync(IClientSessionHandle? session, CancellationToken cancellation = default)
    {
        var previous = await ReadMetadataAsync(session, cancellation).ConfigureAwait(false);
        if (previous.BinaryStorageID == null)
            await FileStorage.EnsureLegacyOwnershipAsync(_db, _db.CollectionName<T>(), new[] { _parent.ID }, session, cancellation).ConfigureAwait(false);
        // A tombstone storage key prevents stale uploads from publishing after explicit deletion.
        if (!await SwitchMetadataAsync(previous.BinaryStorageID, Guid.NewGuid().ToString("N"), 0, 0, false, session, cancellation).ConfigureAwait(false))
            throw new InvalidOperationException("The binary data changed concurrently. Reload and retry.");
        await DeleteChunksAsync(previous.BinaryStorageID, session, cancellation).ConfigureAwait(false);
    }

    Task FlushToDbAsync(IClientSessionHandle? session, StreamInfo streamInfo, bool isLastChunk = false, CancellationToken cancellation = default)
    {
        if (!isLastChunk)
        {
            streamInfo.DataChunk.AddRange(new ArraySegment<byte>(streamInfo.Buffer, 0, streamInfo.ReadCount));
            streamInfo.FileSize += streamInfo.ReadCount;
        }

        if (streamInfo.DataChunk.Count == 0 || (streamInfo.DataChunk.Count < streamInfo.ChunkSize && !isLastChunk))
            return Task.CompletedTask;

        streamInfo.Doc.ID = (string)streamInfo.Doc.GenerateNewID();
        streamInfo.Doc.Data = streamInfo.DataChunk.ToArray();
        streamInfo.DataChunk.Clear();
        streamInfo.ChunkCount++;

        return session == null
                   ? _chunkCollection.InsertOneAsync(streamInfo.Doc, null, cancellation)
                   : _chunkCollection.InsertOneAsync(session, streamInfo.Doc, null, cancellation);
    }

    async Task<bool> SwitchMetadataAsync(string? previousID, string storageID, long size, int count, bool successful,
                                         IClientSessionHandle? session, CancellationToken cancellation)
    {
        var collection = _db.Collection<T>();
        var filter = Builders<T>.Filter.Eq(e => e.ID, _parent.ID) & Builders<T>.Filter.Eq(e => e.BinaryStorageID, previousID);
        var update = Builders<T>.Update.Set(e => e.BinaryStorageID, storageID)
                               .Set(e => e.FileSize, size).Set(e => e.ChunkCount, count).Set(e => e.UploadSuccessful, successful);
        var result = session == null
                         ? await collection.UpdateOneAsync(filter, update, cancellationToken: cancellation).ConfigureAwait(false)
                         : await collection.UpdateOneAsync(session, filter, update, cancellationToken: cancellation).ConfigureAwait(false);
        if (result.MatchedCount != 1)
            return false;
        _parent.BinaryStorageID = storageID;
        _parent.FileSize = size;
        _parent.ChunkCount = count;
        _parent.UploadSuccessful = successful;
        return true;
    }

    async Task<StorageMetadata> ReadMetadataAsync(IClientSessionHandle? session, CancellationToken cancellation)
    {
        var filter = Builders<T>.Filter.Eq(e => e.ID, _parent.ID);
        var options = new FindOptions<T, StorageMetadata>
        {
            Projection = Builders<T>.Projection.Expression(e => new StorageMetadata
            {
                BinaryStorageID = e.BinaryStorageID,
                UploadSuccessful = e.UploadSuccessful
            })
        };
        using var cursor = session == null
                               ? await _db.Collection<T>().FindAsync(filter, options, cancellation).ConfigureAwait(false)
                               : await _db.Collection<T>().FindAsync(session, filter, options, cancellation).ConfigureAwait(false);
        return await cursor.FirstOrDefaultAsync(cancellation).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The file metadata no longer exists.");
    }

    Task DeleteChunksAsync(string? storageID, IClientSessionHandle? session, CancellationToken cancellation = default)
        => FileStorage.DeleteChunksAsync(_chunkCollection, _parent.ID, storageID, session, cancellation);

    sealed class StorageMetadata
    {
        public string? BinaryStorageID { get; set; }
        public bool UploadSuccessful { get; set; }
    }

    sealed class StreamInfo(FileChunk doc, int chunkSize, int readCount, byte[] buffer, List<byte> dataChunk, MD5? md5 = null)
    {
        public long FileSize { get; set; }
        public int ChunkCount { get; set; }
        public FileChunk Doc { get; } = doc;
        public int ChunkSize { get; } = chunkSize;
        public int ReadCount { get; set; } = readCount;
        public byte[] Buffer { get; } = buffer;
        public List<byte> DataChunk { get; } = dataChunk;
        public MD5? Md5 { get; set; } = md5;
    }
}