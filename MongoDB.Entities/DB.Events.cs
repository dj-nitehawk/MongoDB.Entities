using MongoDB.Entities.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        public static void OnInsert<TEntity>(Action<TEntity> action) where TEntity : IEntity
        {
            
        }

        private static void MonitorInserts<TEntity>() where TEntity : IEntity
        {

        }
    }
}
