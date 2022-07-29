namespace MongoDB.Entities.Configuration;

using System.Linq.Expressions;
//TODO: create a proper builder like EF core
public class EntityConfigBuilder<T>
{
    private MongoDbEntityMappingConfiguration _mappingConfiguration;

    public EntityConfigBuilder(MongoDbEntityMappingConfiguration mappingConfiguration)
    {
        _mappingConfiguration = mappingConfiguration;
    }

    public EntityDocumentRelationConfigBuilder<T, TOther> HasOne<TOther>(Expression<Func<T, MongoDBDocumentReference<TOther>>> selector)
    {
        return new(selector);
    }
}

public class EntityDocumentRelationConfigBuilder<T, TOther>
{
    Expression<Func<T, MongoDBDocumentReference<TOther>>> _selector;
    public EntityDocumentRelationConfigBuilder(Expression<Func<T, MongoDBDocumentReference<TOther>>> selector)
    {
        _selector = selector;
    }

    Expression<Func<T, object>>? _idSelector;
    public EntityDocumentRelationConfigBuilder<T, TOther> WithId(Expression<Func<T, object>> idSelector)
    {
        _idSelector = idSelector;
        return this;
    }

    internal void BuildDocumentSide()
    {
        
    }
}