using MongoDB.Entities;
using MongoDB.Entities.Common;

namespace Benchmark.Models
{
    public class BlogPost : Entity
    {
        public string Title { get; set; }
        public string Content { get; set; }

        [OwnerSide]
        public Many<Category> Categories { get; set; }

        public BlogPost()
        {
            this.InitManyToMany(() => Categories, c => c.Posts);
        }
    }
}
