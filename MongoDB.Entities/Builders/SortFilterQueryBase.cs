using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
#nullable enable
namespace MongoDB.Entities
{
    public abstract class SortFilterQueryBase<T, TSelf> : FilterQueryBase<T, TSelf> where T : IEntity where TSelf : SortFilterQueryBase<T, TSelf>
    {
        internal List<SortDefinition<T>> _sorts = new();
        private TSelf This => (TSelf)this;

        internal SortFilterQueryBase(SortFilterQueryBase<T, TSelf> other) : base(other)
        {
            _sorts = other._sorts;
        }
        internal SortFilterQueryBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters: globalFilters)
        {
        }


        /// <summary>
        /// Specify which property and order to use for sorting (use multiple times if needed)
        /// </summary>
        /// <param name="propertyToSortBy">x => x.Prop</param>
        /// <param name="sortOrder">The sort order</param>
        public TSelf Sort(Expression<Func<T, object>> propertyToSortBy, Order sortOrder)
        {
            return sortOrder switch
            {
                Order.Ascending => Sort(s => s.Ascending(propertyToSortBy)),
                Order.Descending => Sort(s => s.Descending(propertyToSortBy)),
                _ => This,
            };
        }

        /// <summary>
        /// Specify how to sort using a sort expression
        /// </summary>
        /// <param name="sortFunction">s => s.Ascending("Prop1").MetaTextScore("Prop2")</param>
        /// <returns></returns>
        public TSelf Sort(Func<SortDefinitionBuilder<T>, SortDefinition<T>> sortFunction)
        {
            _sorts.Add(sortFunction(Builders<T>.Sort));
            return This;
        }
    }
}
