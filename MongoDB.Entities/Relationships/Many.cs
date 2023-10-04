using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

/// <summary>
/// Base class providing shared state for Many'1 classes
/// </summary>
public abstract class ManyBase
{
    //shared state for all Many<T> instances
    internal static ConcurrentBag<string> indexedCollections = new();
    internal static string PropTypeName = typeof(Many<Entity, Entity>).Name;
}

/// <summary>
/// Represents a one-to-many/many-to-many relationship between two Entities.
/// <para>WARNING: You have to initialize all instances of this class before accessing any of it's members.</para>
/// <para>Initialize from the constructor of the parent entity as follows:</para>
/// <para><c>this.InitOneToMany(() => Property);</c></para>
/// <para><c>this.InitManyToMany(() => Property, x => x.OtherProperty);</c></para>
/// </summary>
/// <typeparam name="TChild">Type of the child IEntity.</typeparam>
/// <typeparam name="TParent">The type of the parent</typeparam>
public sealed partial class Many<TChild, TParent> : ManyBase where TChild : IEntity where TParent : IEntity
{
    private static readonly BulkWriteOptions unOrdBlkOpts = new() { IsOrdered = false };
    private bool isInverse;
    private TParent parent = default!;

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
        parent.ThrowIfUnsaved();

        return isInverse
            ? session == null
                   ? JoinCollection.CountDocumentsAsync(j => j.ChildID == parent.GetId(), options, cancellation)
                   : JoinCollection.CountDocumentsAsync(session, j => j.ChildID == parent.GetId(), options, cancellation)
            : session == null
                   ? JoinCollection.CountDocumentsAsync(j => j.ParentID == parent.GetId(), options, cancellation)
                   : JoinCollection.CountDocumentsAsync(session, j => j.ParentID == parent.GetId(), options, cancellation);
    }

    /// <summary>
    /// Creates an instance of Many&lt;TChild&gt; 
    /// This is only needed in VB.Net
    /// </summary>
    public Many() { }

    #region one-to-many-initializers
    internal Many(object parent, string property)
    {
        Init((dynamic)parent, property);
    }

    private void Init(TParent parent, string property)
    {
        if (DB.DatabaseName<TParent>() != DB.DatabaseName<TChild>())
            throw new NotSupportedException("Cross database relationships are not supported!");

        this.parent = parent;
        isInverse = false;
        JoinCollection = DB.GetRefCollection<TParent>($"[{DB.CollectionName<TParent>()}~{DB.CollectionName<TChild>()}({property})]");
        CreateIndexesAsync(JoinCollection);
    }

    /// <summary>
    /// Use this method to initialize the Many&lt;TChild&gt; properties with VB.Net
    /// </summary>
    /// <param name="parent">The parent entity instance</param>
    /// <param name="property">Function(x) x.PropName</param>
    public void VB_InitOneToMany(TParent parent, Expression<Func<TParent, object>> property)
    {
        Init(parent, Prop.Property(property));
    }
    #endregion

    #region many-to-many initializers
    internal Many(object parent, string propertyParent, string propertyChild, bool isInverse)
    {
        Init((dynamic)parent, propertyParent, propertyChild, isInverse);
    }

    private void Init(TParent parent, string propertyParent, string propertyChild, bool isInverse)
    {
        this.parent = parent;
        this.isInverse = isInverse;

        JoinCollection = isInverse
            ? DB.GetRefCollection<TParent>($"[({propertyParent}){DB.CollectionName<TChild>()}~{DB.CollectionName<TParent>()}({propertyChild})]")
            : DB.GetRefCollection<TParent>($"[({propertyChild}){DB.CollectionName<TParent>()}~{DB.CollectionName<TChild>()}({propertyParent})]");

        CreateIndexesAsync(JoinCollection);
    }

    /// <summary>
    /// Use this method to initialize the Many&lt;TChild&gt; properties with VB.Net
    /// </summary>
    /// <param name="parent">The parent entity instance</param>
    /// <param name="propertyParent">Function(x) x.ParentProp</param>
    /// <param name="propertyChild">Function(x) x.ChildProp</param>
    /// <param name="isInverse">Specify if this is the inverse side of the relationship or not</param>
    public void VB_InitManyToMany(
        TParent parent,
        Expression<Func<TParent, object>> propertyParent,
        Expression<Func<TChild, object>> propertyChild,
        bool isInverse)
    {
        Init(parent, Prop.Property(propertyParent), Prop.Property(propertyChild), isInverse);
    }
    #endregion

    private static Task CreateIndexesAsync(IMongoCollection<JoinRecord> collection)
    {
        //only create indexes once (best effort) per unique ref collection
        if (!indexedCollections.Contains(collection.CollectionNamespace.CollectionName))
        {
            indexedCollections.Add(collection.CollectionNamespace.CollectionName);
            collection.Indexes.CreateManyAsync(
                new[] {
                    new CreateIndexModel<JoinRecord>(
                        Builders<JoinRecord>.IndexKeys.Ascending(r => r.ParentID),
                        new CreateIndexOptions
                        {
                            Background = true,
                            Name = "[ParentID]"
                        })
                    ,
                    new CreateIndexModel<JoinRecord>(
                        Builders<JoinRecord>.IndexKeys.Ascending(r => r.ChildID),
                        new CreateIndexOptions
                        {
                            Background = true,
                            Name = "[ChildID]"
                        })
                });
        }
        return Task.CompletedTask;
    }
}
