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
public abstract class FileEntity<T> : Entity where T : FileEntity<T>, new()
{
    DataStreamer<T>? _streamer;

    /// <summary>
    /// The total amount of data in bytes that has been uploaded so far
    /// </summary>
    [BsonElement]
    public long FileSize { get; internal set; }

    /// <summary>
    /// The number of chunks that have been created so far
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

    /// <summary>
    /// Access the DataStreamer class for uploading and downloading data
    /// </summary>
    /// <param name="database">The database instance to use for this operation</param>
    public DataStreamer<T> Data(DB database)
        => _streamer ??= new(this, database);
}

[Collection("[BINARY_CHUNKS]")]
sealed class FileChunk : IEntity
{
    [BsonId, ObjectId]
    public string ID { get; set; } = null!;

    [AsObjectId]
    public string FileID { get; set; } = null!;

    public byte[] Data { get; set; } = [];

    public object GenerateNewID()
        => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
        => string.IsNullOrEmpty(ID);
}

/// <summary>
/// Provides the interface for uploading and downloading data chunks for file entities.
/// </summary>
public class DataStreamer<T> where T : FileEntity<T>, new()
{
    static readonly HashSet<string> _indexedDBs = [];

    readonly FileEntity<T> _parent;
    readonly IMongoDatabase _mongoDatabase;
    readonly DB _db;
    readonly IMongoCollection<FileChunk> _chunkCollection;

    internal DataStreamer(FileEntity<T> parent, DB db)
    {
        _parent = parent;
        _db = db;
        _mongoDatabase = _db.Database();
        _chunkCollection = _db.Collection<FileChunk>();

        if (_indexedDBs.Add(_mongoDatabase.DatabaseNamespace.DatabaseName))
        {
            _ = _chunkCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<FileChunk>(
                    Builders<FileChunk>.IndexKeys.Ascending(c => c.FileID),
                    new() { Background = true, Name = $"{nameof(FileChunk.FileID)}(Asc)" }));
        }
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

        if (!_parent.UploadSuccessful)
            throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");
        if (!stream.CanWrite)
            throw new NotSupportedException("The supplied stream is not writable!");

        var filter = Builders<FileChunk>.Filter.Eq(c => c.FileID, _parent.ID);
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

        if (chunkSizeKb is < 128 or > 4096)
            throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
        if (!stream.CanRead)
            throw new NotSupportedException("The supplied stream is not readable!");

        // ReSharper disable once MethodSupportsCancellation
        await CleanUpAsync(session).ConfigureAwait(false);

        var chunkSize = chunkSizeKb * 1024;
        var streamInfo = new StreamInfo(
            new() { FileID = _parent.ID },
            chunkSize,
            0,
            new byte[64 * 1024],
            new(chunkSize));

        if (!string.IsNullOrEmpty(_parent.Md5))
            streamInfo.Md5 = MD5.Create();

        try
        {
            if (stream is { CanSeek: true, Position: > 0 })
                stream.Position = 0;

            while ((streamInfo.ReadCount = await stream.ReadAsync(streamInfo.Buffer, 0, streamInfo.Buffer.Length, cancellation).ConfigureAwait(false)) > 0)
            {
                streamInfo.Md5?.TransformBlock(streamInfo.Buffer, 0, streamInfo.ReadCount, null, 0);
                await FlushToDbAsync(session, streamInfo, isLastChunk: false, cancellation).ConfigureAwait(false);
            }

            if (_parent.FileSize > 0)
            {
                streamInfo.Md5?.TransformFinalBlock(streamInfo.Buffer, 0, streamInfo.ReadCount);

                if (streamInfo.Md5 != null &&
                    !BitConverter.ToString(streamInfo.Md5.Hash).Replace("-", "").Equals(_parent.Md5, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("MD5 of uploaded data doesn't match with file entity MD5.");

                await FlushToDbAsync(session, streamInfo, isLastChunk: true, cancellation).ConfigureAwait(false);
                _parent.UploadSuccessful = true;
            }
            else
                throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
        }
        catch (Exception)
        {
            // ReSharper disable once MethodSupportsCancellation
            await CleanUpAsync(session).ConfigureAwait(false);

            throw;
        }
        finally
        {
            await UpdateMetaDataAsync(session).ConfigureAwait(false);
            streamInfo.Md5?.Dispose();
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

        return cancellation != CancellationToken.None && session == null
                   ? throw new NotSupportedException("Cancellation is only supported within transactions for deleting binary chunks!")
                   : CleanUpAsync(session, cancellation);
    }

    Task CleanUpAsync(IClientSessionHandle? session, CancellationToken cancellation = default)
    {
        _parent.FileSize = 0;
        _parent.ChunkCount = 0;
        _parent.UploadSuccessful = false;

        return session == null
                   ? _chunkCollection.DeleteManyAsync(c => c.FileID == _parent.ID, cancellation)
                   : _chunkCollection.DeleteManyAsync(session, c => c.FileID == _parent.ID, null, cancellation);
    }

    Task FlushToDbAsync(IClientSessionHandle? session, StreamInfo streamInfo, bool isLastChunk = false, CancellationToken cancellation = default)
    {
        if (!isLastChunk)
        {
            streamInfo.DataChunk.AddRange(new ArraySegment<byte>(streamInfo.Buffer, 0, streamInfo.ReadCount));
            _parent.FileSize += streamInfo.ReadCount;
        }

        if (streamInfo.DataChunk.Count < streamInfo.ChunkSize && !isLastChunk)
            return Task.CompletedTask;

        streamInfo.Doc.ID = (string)streamInfo.Doc.GenerateNewID();
        streamInfo.Doc.Data = streamInfo.DataChunk.ToArray();
        streamInfo.DataChunk.Clear();
        _parent.ChunkCount++;

        return session == null
                   ? _chunkCollection.InsertOneAsync(streamInfo.Doc, null, cancellation)
                   : _chunkCollection.InsertOneAsync(session, streamInfo.Doc, null, cancellation);
    }

    Task UpdateMetaDataAsync(IClientSessionHandle? session)
    {
        var collection = _mongoDatabase.GetCollection<FileEntity<T>>(_db.CollectionName<T>());
        var filter = Builders<FileEntity<T>>.Filter.Eq(e => e.ID, _parent.ID);
        var update = Builders<FileEntity<T>>.Update
                                            .Set(e => e.FileSize, _parent.FileSize)
                                            .Set(e => e.ChunkCount, _parent.ChunkCount)
                                            .Set(e => e.UploadSuccessful, _parent.UploadSuccessful);

        return session == null
                   ? collection.UpdateOneAsync(filter, update)
                   : collection.UpdateOneAsync(session, filter, update);
    }

    struct StreamInfo(FileChunk doc, int chunkSize, int readCount, byte[] buffer, List<byte> dataChunk, MD5? md5 = null)
    {
        public FileChunk Doc { get; } = doc;
        public int ChunkSize { get; } = chunkSize;
        public int ReadCount { get; set; } = readCount;
        public byte[] Buffer { get; } = buffer;
        public List<byte> DataChunk { get; } = dataChunk;
        public MD5? Md5 { get; set; } = md5;
    }
}