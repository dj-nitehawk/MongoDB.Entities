
using MongoDB.Entities;
using MongoDB.Entities.Core;

namespace Benchmark.Models
{
    public class Category : Entity
    {
        public string Name { get; set; }

        [InverseSide]
        public Many<BlogPost> Posts { get; set; }

        public Category()
        {
            this.InitManyToMany(() => Posts, b => b.Categories);
        }
    }
}
