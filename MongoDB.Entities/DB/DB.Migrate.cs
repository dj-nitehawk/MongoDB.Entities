﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Discover and run migrations from the same assembly as the specified type.
        /// </summary>
        /// <typeparam name="T">A type that is from the same assembly as the migrations you want to run</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task MigrateAsync<T>(string tenantPrefix) where T : class
        {
            return Migrate(typeof(T), tenantPrefix);
        }

        /// <summary>
        /// Executes migration classes that implement the IMigration interface in the correct order to transform the database.
        /// <para>TIP: Write classes with names such as: _001_rename_a_field.cs, _002_delete_a_field.cs, etc. 
        /// and implement IMigration interface on them. 
        /// Call this method at the startup of the application in order to run the migrations.</para>
        /// </summary>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task MigrateAsync(string tenantPrefix)
        {
            return Migrate(null, tenantPrefix);
        }

        /// <summary>
        /// Executes the given collection of IMigrations in the correct order to transform the database.
        /// </summary>
        /// <param name="migrations">The collection of migrations to execute</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task MigrationsAsync(IEnumerable<IMigration> migrations, string tenantPrefix)
        {
            return Execute(migrations, tenantPrefix);
        }

        private static Task Migrate(Type targetType, string tenantPrefix)
        {
            IEnumerable<Assembly> assemblies;

            if (targetType == null)
            {
                var excludes = new[]
                {
                    "Microsoft.",
                    "System.",
                    "MongoDB.",
                    "testhost",
                    "netstandard",
                    "Newtonsoft.",
                    "mscorlib",
                    "NuGet."
                };

                assemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a =>
                          (!a.IsDynamic && !excludes.Any(n => a.FullName.StartsWith(n))) ||
                          a.FullName.StartsWith("MongoDB.Entities.Tests"));
            }
            else
            {
                assemblies = new[] { targetType.Assembly };
            }

            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Contains(typeof(IMigration)));

            if (!types.Any())
                throw new InvalidOperationException("Didn't find any classes that implement IMigrate interface.");

            return Execute(types.Select(t => (IMigration)Activator.CreateInstance(t)), tenantPrefix);
        }

        private static async Task Execute(IEnumerable<IMigration> migrations, string tenantPrefix)
        {
            var lastMigNum = await
                Find<Migration, int>(tenantPrefix)
                .Sort(m => m.Number, Order.Descending)
                .Project(m => m.Number)
                .ExecuteFirstAsync()
                .ConfigureAwait(false);

            var dic = new SortedDictionary<int, (string name, IMigration migration)>();

            foreach (var m in migrations)
            {
                var nameParts = m.GetType().Name.Split('_');

                if (nameParts == null)
                    throw new InvalidOperationException("Please use the correct naming format for migration classes!");

                if (!int.TryParse(nameParts[1], out int migNumber))
                    throw new InvalidOperationException("Failed to parse migration number from the class name. Make sure to name the migration classes like: _001_some_migration_name.cs");

                var name = string.Join(" ", nameParts.Skip(2));

                if (migNumber > lastMigNum)
                    dic.Add(migNumber, (name, m));
            }

            var sw = new Stopwatch();

            foreach (var migration in dic)
            {
                sw.Start();
                await migration.Value.migration.UpgradeAsync().ConfigureAwait(false);
                var mig = new Migration
                {
                    Number = migration.Key,
                    Name = migration.Value.name,
                    TimeTakenSeconds = sw.Elapsed.TotalSeconds
                };
                await SaveAsync(mig, tenantPrefix).ConfigureAwait(false);
                sw.Stop();
                sw.Reset();
            }
        }
    }
}
