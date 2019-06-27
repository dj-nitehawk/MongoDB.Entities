using Benchmark.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark
{
    internal class Program
    {
        private static readonly int totalIterations = 10000;
        private static readonly int parallelTasks = 32;

        private static void Main(string[] args)
        {
            Start().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }

        private static async Task Start()
        {
            new DB("mongodb-entities-benchmark");
            Console.WriteLine("Total posts in collection: " + (await DB.Queryable<BlogPost>().CountAsync()).ToString());

            var mainWatch = new Stopwatch();
            var batchWatch = new Stopwatch();

            mainWatch.Start();
            batchWatch.Start();

            for (int i = 0; i < totalIterations; i += parallelTasks)
            {
                var tasks = new List<Task>();
                batchWatch.Restart();

                for (int x = 1; x <= parallelTasks; x++)
                {
                    var postNum = i + x;
                    tasks.Add(Task.Run(() => DoWork(postNum)));
                    if (postNum == totalIterations) break;
                }

                await Task.WhenAll(tasks);
                Console.WriteLine("Batch completed in: " + batchWatch.Elapsed.TotalSeconds.ToString() + " seconds");
            }

            Console.WriteLine("ALL COMPLETED IN: " + mainWatch.Elapsed.TotalSeconds.ToString() + " seconds");
            Console.WriteLine("Total posts in collection: " + (await DB.Queryable<BlogPost>().CountAsync()).ToString());
        }

        private static async Task DoWork(int iteration)
        {
            var post = new BlogPost
            {
                Title = $"blog post number: {iteration} [thread: {Thread.CurrentThread.ManagedThreadId}]",
                Content = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
            };
            await post.SaveAsync();

            var cat1 = new Category { Name = $"cat one for iteration {iteration}" };
            await cat1.SaveAsync();

            var cat2 = new Category { Name = $"cat two for iteration {iteration}" };
            await cat2.SaveAsync();

            await post.Categories.AddAsync(cat1);
            await cat2.Posts.AddAsync(post);

            var resCat = await cat2.Queryable().Where(c => c.ID == cat2.ID).SingleAsync();
            if (resCat.Name != cat2.Name) throw new Exception("this is the wrong category");

            var resPost = await resCat.Posts.ChildrenQueryable().SingleAsync();
            if (resPost.Title != post.Title) throw new Exception("this is the wrong post");
        }
    }
}
