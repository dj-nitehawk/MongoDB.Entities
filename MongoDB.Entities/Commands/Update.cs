using MongoDB.Driver;
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a batch update command
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Set() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that inhertis from Entity</typeparam>
    public class Update<T> where T : Entity
    {
        private Collection<UpdateDefinition<T>> defs = new Collection<UpdateDefinition<T>>();
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private UpdateOptions options = new UpdateOptions();
        private IClientSessionHandle session = null;

        internal Update(IClientSessionHandle session = null) => this.session = session;

        /// <summary>
        /// Specify the Entity matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">A lambda expression to select the Entities to update</param>
        public Update<T> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the Entity matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Update<T> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter = filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the property and it's value to modify (use multiple times if needed)
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        /// <returns></returns>
        public Update<T> Modify<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            defs.Add(Builders<T>.Update.Set(property, value));
            return this;
        }

        /// <summary>
        /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
        /// <returns></returns>
        public Update<T> Modify(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
        {
            defs.Add(operation(Builders<T>.Update));
            return this;
        }

        /// <summary>
        /// Specify an option for this update command (use multiple times if needed)
        /// <para>TIP: Setting options is not required</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Update<T> Option(Action<UpdateOptions> option)
        {
            option(options);
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
            if (filter == null) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Set() method first!");
            await DB.UpdateAsync(filter, Builders<T>.Update.Combine(defs), options, session);
        }
    }
}
