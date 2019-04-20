using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace MongoDAL
{
    public class MongoRef<T> where T : MongoEntity
    {
        [MongoRef]
        public string Id { get; set; }

        [MongoIgnore]
        public T Entity
        {
            get
            {
                return DB.Collection<T>().SingleOrDefault(t => t.Id.Equals(Id));
            }
        }

        public MongoRef(T entity)
        {
            if (string.IsNullOrEmpty(entity.Id)) throw new InvalidOperationException("Please save the entity before adding references to it!");
            Id = entity.Id;
        }
    }

    public class MongoRefList<T> where T : MongoEntity
    {

    }
}
