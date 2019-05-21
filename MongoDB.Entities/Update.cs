using MongoDB.Driver;
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a batch update command
    /// <para>TIP: Specify a filter first with the .Match() method. Then set property values with .Set() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that inhertis from Entity</typeparam>
    public class Update<T> where T : Entity
    {
        private Collection<UpdateDefinition<T>> _defs = new Collection<UpdateDefinition<T>>();
        private Expression<Func<T, bool>> _filter = null;

        /// <summary>
        /// Specify the Entity matching criteria
        /// </summary>
        /// <param name="expression">A lambda expression to select the Entities to update</param>
        /// <returns></returns>
        public Update<T> Match(Expression<Func<T, bool>> expression)
        {
            _filter = expression;
            return this;
        }

        /// <summary>
        /// Specify the property and it's value to set.
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        /// <returns></returns>
        public Update<T> Set<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            _defs.Add(Builders<T>.Update.Set(property, value));
            return this;
        }

        /// <summary>
        /// Run the batch update command in MongoDB.
        /// </summary>
        public void Execute()
        {
            ExecuteAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Run the batch update command in MongoDB.
        /// </summary>
        async public Task ExecuteAsync()

        {
            if (_filter == null) throw new ArgumentException("Please use Match() method first!");
            if (_defs.Count == 0) throw new ArgumentException("Please use Set() method first!");
            await DB.UpdateAsync<T>(_filter, Builders<T>.Update.Combine(_defs));
        }
    }
}
