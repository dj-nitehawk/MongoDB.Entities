using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// setting this to true will cause any global filters to be ignored when actions are performed with this DB instance.
    /// </summary>
    public bool IgnoreGlobalFilters { get; set; }

    static Type[]? _allEntityTypes;
    Dictionary<Type, (object filterDef, bool prepend)>? _globalFilters;

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
    /// <param name="filter">x => x.Prop1 == "some value"</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilter<T>(Expression<Func<T, bool>> filter, bool prepend = false) where T : IEntity
    {
        SetGlobalFilter(Builders<T>.Filter.Where(filter), prepend);
    }

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
    /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilter<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, bool prepend = false) where T : IEntity
    {
        SetGlobalFilter(filter(Builders<T>.Filter), prepend);
    }

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
    /// <param name="filter">A filter definition to be applied</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilter<T>(FilterDefinition<T> filter, bool prepend = false) where T : IEntity
    {
        AddFilter(typeof(T), (filter, prepend));
    }

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <param name="type">The type of Entity this global filter should be applied to</param>
    /// <param name="jsonString">A JSON string filter definition to be applied</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilter(Type type, string jsonString, bool prepend = false)
    {
        AddFilter(type, (jsonString, prepend));
    }

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <typeparam name="TBase">The type of the base class</typeparam>
    /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilterForBaseClass<TBase>(Expression<Func<TBase, bool>> filter, bool prepend = false) where TBase : IEntity
    {
        SetGlobalFilterForBaseClass(Builders<TBase>.Filter.Where(filter), prepend);
    }

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <typeparam name="TBase">The type of the base class</typeparam>
    /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilterForBaseClass<TBase>(Func<FilterDefinitionBuilder<TBase>, FilterDefinition<TBase>> filter, bool prepend = false)
        where TBase : IEntity
    {
        SetGlobalFilterForBaseClass(filter(Builders<TBase>.Filter), prepend);
    }

    /// <summary>
    /// Specify a global filter to be applied to all operations performed with this DBContext
    /// </summary>
    /// <typeparam name="TBase">The type of the base class</typeparam>
    /// <param name="filter">A filter definition to be applied</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilterForBaseClass<TBase>(FilterDefinition<TBase> filter, bool prepend = false) where TBase : IEntity
    {
        _allEntityTypes ??= GetAllEntityTypes();

        foreach (var entType in _allEntityTypes.Where(t => t.IsSubclassOf(typeof(TBase))))
        {
            var bsonDoc = filter.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<TBase>(), BsonSerializer.SerializerRegistry));

            AddFilter(entType, (bsonDoc, prepend));
        }
    }

    /// <summary>
    /// Specify a global filter for all entity types that implements a given interface
    /// </summary>
    /// <typeparam name="TInterface">The interface type to target. Will throw if supplied argument is not an interface type</typeparam>
    /// <param name="jsonString">A JSON string filter definition to be applied</param>
    /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
    protected void SetGlobalFilterForInterface<TInterface>(string jsonString, bool prepend = false)
    {
        var targetType = typeof(TInterface);

        if (!targetType.IsInterface)
            throw new ArgumentException("Only interfaces are allowed!", nameof(TInterface));

        _allEntityTypes ??= GetAllEntityTypes();

        foreach (var entType in _allEntityTypes.Where(targetType.IsAssignableFrom))
            AddFilter(entType, (jsonString, prepend));
    }

    static Type[] GetAllEntityTypes()
    {
        var excludes = new[]
        {
            "Microsoft.",
            "System.",
            "MongoDB.",
            "testhost.",
            "netstandard",
            "Newtonsoft.",
            "mscorlib",
            "NuGet."
        };

        return AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(
                            a => !a.IsDynamic &&
                                 (a.FullName.StartsWith("MongoDB.Entities.Tests") || !excludes.Any(n => a.FullName.StartsWith(n))))
                        .SelectMany(a => a.GetTypes())
                        .Where(t => typeof(IEntity).IsAssignableFrom(t))
                        .ToArray();
    }

    void AddFilter(Type type, (object filterDef, bool prepend) filter)
    {
        _globalFilters ??= new();

        _globalFilters[type] = filter;
    }
}