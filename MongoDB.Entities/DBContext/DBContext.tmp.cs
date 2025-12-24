using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBContext
{
    DB _db;
    public IClientSessionHandle? Session { get; protected set; }
    Dictionary<Type, (object filterDef, bool prepend)>? _globalFilters;
}