using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public class DistinctBase<T, TProperty, TSelf> : FilterQueryBase<T, TSelf> where T : IEntity where TSelf : DistinctBase<T, TProperty, TSelf>
    {
        internal DistinctOptions _options = new();
        internal FieldDefinition<T, TProperty>? _field;

        internal DistinctBase(DistinctBase<T, TProperty, TSelf> other) : base(other)
        {
            _options = other._options;
            _field = other._field;
        }
        internal DistinctBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters)
        {
            _globalFilters = globalFilters;
        }


        private TSelf This => (TSelf)this;


        /// <summary>
        /// Specify an option for this find command (use multiple times if needed)
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public TSelf Option(Action<DistinctOptions> option)
        {
            option(_options);
            return This;
        }

        /// <summary>
        /// Specify the property you want to get the unique values for (as a string path)
        /// </summary>
        /// <param name="property">ex: "Address.Street"</param>
        public TSelf Property(string property)
        {
            _field = property;
            return This;
        }

        /// <summary>
        /// Specify the property you want to get the unique values for (as a member expression)
        /// </summary>
        /// <param name="property">x => x.Address.Street</param>
        public TSelf Property(Expression<Func<T, object>> property)
        {
            _field = property.FullPath();
            return This;
        }
    }

    /// <summary>
    /// Represents a MongoDB Distinct command where you can get back distinct values for a given property of a given Entity.
    /// </summary>
    /// <typeparam name="T">Any Entity that implements IEntity interface</typeparam>
    /// <typeparam name="TProperty">The type of the property of the entity you'd like to get unique values for</typeparam>
    public class Distinct<T, TProperty> : DistinctBase<T, TProperty, Distinct<T, TProperty>>, ICollectionRelated<T> where T : IEntity
    {
        public DBContext Context { get; }
        public IMongoCollection<T> Collection { get; }

        internal Distinct(
            DBContext context,
            IMongoCollection<T> collection,
            DistinctBase<T, TProperty, Distinct<T, TProperty>> other) : base(other)
        {
            Context = context;
            Collection = collection;
        }
        internal Distinct(
            DBContext context,
            IMongoCollection<T> collection,
            Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters: globalFilters)
        {
            Context = context;
            Collection = collection;
        }

        /// <summary>
        /// Run the Distinct command in MongoDB server and get a cursor instead of materialized results
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<IAsyncCursor<TProperty>> ExecuteCursorAsync(CancellationToken cancellation = default)
        {
            if (_field == null)
                throw new InvalidOperationException("Please use the .Property() method to specify the field to use for obtaining unique values for!");

            var mergedFilter = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);

            return Context.Session is IClientSessionHandle session
                   ? Collection.DistinctAsync(session, _field, mergedFilter, _options, cancellation)
                   : Collection.DistinctAsync(_field, mergedFilter, _options, cancellation);
        }

        /// <summary>
        /// Run the Distinct command in MongoDB server and get a list of unique property values
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<List<TProperty>> ExecuteAsync(CancellationToken cancellation = default)
        {
            var list = new List<TProperty>();
            using (var csr = await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
            {
                while (await csr.MoveNextAsync(cancellation).ConfigureAwait(false))
                {
                    list.AddRange(csr.Current);
                }
            }
            return list;
        }
    }
}
