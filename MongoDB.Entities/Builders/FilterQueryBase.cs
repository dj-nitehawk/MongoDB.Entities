using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
namespace MongoDB.Entities
{
    public abstract class FilterQueryBase<T, TSelf> where T : IEntity where TSelf : FilterQueryBase<T, TSelf>
    {
        internal FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
        internal Dictionary<Type, (object filterDef, bool prepend)> _globalFilters;
        internal bool _ignoreGlobalFilters;

        internal FilterQueryBase(FilterQueryBase<T, TSelf> other) : this(globalFilters: other._globalFilters)
        {
            _filter = other._filter;
            _ignoreGlobalFilters = other._ignoreGlobalFilters;
        }
        internal FilterQueryBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters)
        {
            _globalFilters = globalFilters;
        }

        protected FilterDefinition<T> MergedFilter => Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);
        private TSelf This => (TSelf)this;


        /// <summary>
        /// Specify that this operation should ignore any global filters
        /// </summary>
        public TSelf IgnoreGlobalFilters()
        {
            _ignoreGlobalFilters = true;
            return This;
        }



        /// <summary>
        /// Specify an IEntity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique IEntity ID</param>
        public TSelf Match(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
        }

        /// <summary>
        /// Specify the matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        public TSelf Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public TSelf Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            _filter &= filter(Builders<T>.Filter);
            return This;
        }

        /// <summary>
        /// Specify the matching criteria with a filter definition
        /// </summary>
        /// <param name="filterDefinition">A filter definition</param>
        public TSelf Match(FilterDefinition<T> filterDefinition)
        {
            _filter &= filterDefinition;
            return This;
        }

        /// <summary>
        /// Specify the matching criteria with a template
        /// </summary>
        /// <param name="template">A Template with a find query</param>
        public TSelf Match(Template template)
        {
            _filter &= template.RenderToString();
            return This;
        }

        /// <summary>
        /// Specify a search term to find results from the text index of this particular collection.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="searchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        public TSelf Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null)
        {
            if (searchType == Search.Fuzzy)
            {
                searchTerm = searchTerm.ToDoubleMetaphoneHash();
                caseSensitive = false;
                diacriticSensitive = false;
                language = null;
            }

            return Match(
                f => f.Text(
                    searchTerm,
                    new TextSearchOptions
                    {
                        CaseSensitive = caseSensitive,
                        DiacriticSensitive = diacriticSensitive,
                        Language = language
                    }));
        }

        /// <summary>
        /// Specify criteria for matching entities based on GeoSpatial data (longitude &amp; latitude)
        /// <para>TIP: Make sure to define a Geo2DSphere index with DB.Index&lt;T&gt;() before searching</para>
        /// <para>Note: DB.FluentGeoNear() supports more advanced options</para>
        /// </summary>
        /// <param name="coordinatesProperty">The property where 2DCoordinates are stored</param>
        /// <param name="nearCoordinates">The search point</param>
        /// <param name="maxDistance">Maximum distance in meters from the search point</param>
        /// <param name="minDistance">Minimum distance in meters from the search point</param>
        public TSelf Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
        {
            return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
        }

        /// <summary>
        /// Specify the matching criteria with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        public TSelf MatchExpression(string expression)
        {
            _filter &= "{$expr:" + expression + "}";
            return This;
        }

        /// <summary>
        /// Specify the matching criteria with a Template
        /// </summary>
        /// <param name="template">A Template object</param>
        public TSelf MatchExpression(Template template)
        {
            return MatchExpression(template.RenderToString());
        }

        /// <summary>
        /// Specify an IEntity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique IEntity ID</param>
        public TSelf MatchID(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
        }

        /// <summary>
        /// Specify the matching criteria with a JSON string
        /// </summary>
        /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
        public TSelf MatchString(string jsonString)
        {
            _filter &= jsonString;
            return This;
        }
    }
}
