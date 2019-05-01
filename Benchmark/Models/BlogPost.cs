using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Entities;

namespace Benchmark.Models
{
    public class BlogPost : Entity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public Many<Category> Categories { get; set; }

        public BlogPost()
        {
            this.InitManyToMany(() => Categories, c => c.Posts, Side.Owner);
        }
    }
}
