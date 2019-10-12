using MongoDB.Driver;
using MongoDB.Entities.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents an update command
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Update<T> where T : IEntity
    {
        private readonly Collection<UpdateDefinition<T>> defs = new Collection<UpdateDefinition<T>>();
        private readonly Collection<PipelineStageDefinition<T, T>> stages = new Collection<PipelineStageDefinition<T, T>>();
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private UpdateOptions options = new UpdateOptions();
        private readonly IClientSessionHandle session = null;
        private readonly Collection<UpdateManyModel<T>> models = new Collection<UpdateManyModel<T>>();
        private string db = null;

        internal Update(IClientSessionHandle session = null, string db = null)
        {
            this.session = session;
            this.db = db;
        }

        /// <summary>
        /// Specify the IEntity matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">A lambda expression to select the Entities to update</param>
        public Update<T> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the IEntity matching criteria with a filter expression
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
        /// Specify an update pipeline stage to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="stage">{ $set: { FullName: { $concat: ['$Name', ' ', '$Surname'] } } }</param>
        public Update<T> WithPipelineStage(string stage)
        {
            stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Specify an array filter to target nested entities for updates with the .Modify() method (use multiple times if needed).
        /// </summary>
        /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
        /// <returns></returns>
        public Update<T> WithArrayFilter(string filter)
        {
            ArrayFilterDefinition<T> def = filter;
            var arrFilters = options.ArrayFilters == null ? new List<ArrayFilterDefinition>() : options.ArrayFilters.ToList();
            arrFilters.Add(def);
            options.ArrayFilters = arrFilters;
            return this;
        }

        /// <summary>
        /// Specify an update (json string) to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
        public Update<T> Modify(string update)
        {
            defs.Add(update);
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
        /// Queue up an update command for bulk execution later.
        /// </summary>
        public Update<T> AddToQueue()
        {
            if (filter == null) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
            Modify(b => b.CurrentDate(x => x.ModifiedOn));
            models.Add(new UpdateManyModel<T>(filter, Builders<T>.Update.Combine(defs)) { ArrayFilters = options.ArrayFilters });
            filter = Builders<T>.Filter.Empty;
            defs.Clear();
            options = new UpdateOptions();
            return this;
        }

        /// <summary>
        /// Run the update command in MongoDB.
        /// </summary>
        public void Execute()
        {
            ExecuteAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Run the update command in MongoDB.
        /// </summary>
        public async Task ExecuteAsync()
        {
            if (models.Count > 0)
            {
                await DB.BulkUpdateAsync(models, session, db);
                models.Clear();
            }
            else
            {
                if (filter == null) throw new ArgumentException("Please use Match() method first!");
                if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
                Modify(b => b.CurrentDate(x => x.ModifiedOn));
                await DB.UpdateAsync(filter, Builders<T>.Update.Combine(defs), options, session, db);
            }
        }

        /// <summary>
        /// Run the update command with pipeline stages
        /// </summary>
        public void ExecutePipeline()
        {
            ExecutePipelineAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Run the update command with pipeline stages
        /// </summary>
        public async Task ExecutePipelineAsync()
        {
            if (filter == null) throw new ArgumentException("Please use Match() method first!");
            if (stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");

            WithPipelineStage($"{{ $set: {{ '{nameof(IEntity.ModifiedOn)}': new Date() }} }}");
            await DB.UpdateAsync(filter, Builders<T>.Update.Pipeline(stages.ToArray()), options, session, db);
        }
    }
}
