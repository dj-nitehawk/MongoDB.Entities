using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Base class providing shared state for Many classes
/// </summary>
public abstract class ManyBase
{
    //shared state for all Many<T> instances
    internal static readonly ConcurrentBag<string> IndexedCollections = [];
    internal static readonly string PropTypeName = typeof(Many<Entity, Entity>).Name;
}

/// <summary>
/// Represents a one-to-many/many-to-many relationship between two Entities.
/// <para>WARNING: You have to initialize all instances of this class before accessing any of its members.</para>
/// <para>Initialize from the constructor of the parent entity as follows:</para>
/// <para>
///     <c>this.InitOneToMany(() => Property);</c>
/// </para>
/// <para>
///     <c>this.InitManyToMany(() => Property, x => x.OtherProperty);</c>
/// </para>
/// </summary>
/// <typeparam name="TChild">Type of the child IEntity.</typeparam>
/// <typeparam name="TParent">The type of the parent</typeparam>
public sealed partial class Many<TChild, TParent> : ManyBase where TChild : IEntity where TParent : IEntity
{
    static readonly BulkWriteOptions _unOrdBlkOpts = new() { IsOrdered = false };
    bool _isInverse;
    TParent _parent = default!;
    readonly DB _db;

    /// <summary>
    /// Gets the IMongoCollection of JoinRecords for this relationship.
    /// <para>TIP: Try never to use this unless really necessary.</para>
    /// </summary>
    public IMongoCollection<JoinRecord> JoinCollection { get; private set; } = null!;

    /// <summary>
    /// Get the number of children for a relationship
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<long> ChildrenCountAsync(IClientSessionHandle? session = null, CountOptions? options = null, CancellationToken cancellation = default)
    {
        _parent.ThrowIfUnsaved();

        return _isInverse
                   ? session == null
                         ? JoinCollection.CountDocumentsAsync(j => j.ChildID == _parent.GetId(), options, cancellation)
                         : JoinCollection.CountDocumentsAsync(session, j => j.ChildID == _parent.GetId(), options, cancellation)
                   : session == null
                       ? JoinCollection.CountDocumentsAsync(j => j.ParentID == _parent.GetId(), options, cancellation)
                       : JoinCollection.CountDocumentsAsync(session, j => j.ParentID == _parent.GetId(), options, cancellation);
    }

    /// <summary>
    /// Creates an instance of Many&lt;TChild&gt;
    /// This is only needed in VB.Net
    /// </summary>
    public Many()
    {
        _db = DB.Default;
    }

#region one-to-many-initializers

    internal Many(object parent, string property, DB? db = null)
    {
        _db = DB.InstanceOrDefault(db);
        Init((TParent)parent, property);
    }

    void Init(TParent parent, string property)
    {
        _parent = parent;
        _isInverse = false;
        var collectionName = $"[{_db.CollectionName<TParent>()}~{_db.CollectionName<TChild>()}({property})]";
        JoinCollection = _db.GetRefCollection<TParent>(collectionName);
        CreateIndexesAsync(JoinCollection);
        Cache<TParent>.AddReferenceCollection(collectionName, JoinCollection);
    }

    /// <summary>
    /// Use this method to initialize the Many&lt;TChild&gt; properties with VB.Net
    /// </summary>
    /// <param name="parent">The parent entity instance</param>
    /// <param name="property">Function(x) x.PropName</param>
    public void VB_InitOneToMany(TParent parent, Expression<Func<TParent, object?>> property)
        => Init(parent, Prop.Property(property));

#endregion

#region many-to-many initializers

    internal Many(object parent, string propertyParent, string propertyChild, bool isInverse, DB? db = null)
    {
        _db = DB.InstanceOrDefault(db);
        Init((TParent)parent, propertyParent, propertyChild, isInverse);
    }

    void Init(TParent parent, string propertyParent, string propertyChild, bool isInverse)
    {
        _parent = parent;
        _isInverse = isInverse;

        var collectionName = isInverse
                                 ? $"[({propertyParent}){_db.CollectionName<TChild>()}~{_db.CollectionName<TParent>()}({propertyChild})]"
                                 : $"[({propertyChild}){_db.CollectionName<TParent>()}~{_db.CollectionName<TChild>()}({propertyParent})]";

        JoinCollection = _db.GetRefCollection<TParent>(collectionName);

        CreateIndexesAsync(JoinCollection);
        Cache<TParent>.AddReferenceCollection(collectionName, JoinCollection);
    }

    /// <summary>
    /// Use this method to initialize the Many&lt;TChild&gt; properties with VB.Net
    /// </summary>
    /// <param name="parent">The parent entity instance</param>
    /// <param name="propertyParent">Function(x) x.ParentProp</param>
    /// <param name="propertyChild">Function(x) x.ChildProp</param>
    /// <param name="isInverse">Specify if this is the inverse side of the relationship or not</param>
    public void VB_InitManyToMany(TParent parent,
                                  Expression<Func<TParent, object?>> propertyParent,
                                  Expression<Func<TChild, object?>> propertyChild,
                                  bool isInverse)
    {
        Init(parent, Prop.Property(propertyParent), Prop.Property(propertyChild), isInverse);
    }

#endregion

    // ReSharper disable once UnusedMethodReturnValue.Local
    static Task CreateIndexesAsync(IMongoCollection<JoinRecord> collection)
    {
        //only create indexes once (best effort) per unique ref collection
        if (IndexedCollections.Contains(collection.CollectionNamespace.CollectionName))
            return Task.CompletedTask;

        IndexedCollections.Add(collection.CollectionNamespace.CollectionName);
        collection.Indexes.CreateManyAsync(
        [
            new(
                Builders<JoinRecord>.IndexKeys.Ascending(r => r.ParentID),
                new()
                {
                    Background = true,
                    Name = "[ParentID]"
                }),
            new(
                Builders<JoinRecord>.IndexKeys.Ascending(r => r.ChildID),
                new()
                {
                    Background = true,
                    Name = "[ChildID]"
                })
        ]);

        return Task.CompletedTask;
    }
}