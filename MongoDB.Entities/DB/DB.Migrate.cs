using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Discover and run migrations from the same assembly as the specified type.
    /// </summary>
    /// <typeparam name="T">A type that is from the same assembly as the migrations you want to run</typeparam>
    public Task MigrateAsync<T>() where T : class
        => MigrateAsync<T>(default);

    /// <summary>
    /// Discover and run migrations from the same assembly as the specified type.
    /// </summary>
    /// <typeparam name="T">A type from the assembly containing the migrations</typeparam>
    /// <param name="cancellation">The token used for history operations and database migrations</param>
    public Task MigrateAsync<T>(CancellationToken cancellation) where T : class
        => Migrate(typeof(T), cancellation);

    /// <summary>
    /// Executes migration classes implementing IMigration or IDatabaseMigration in numeric order.
    /// <para>
    /// TIP: Write classes with names such as: _001_rename_a_field.cs, _002_delete_a_field.cs, etc.
    /// and implement either migration interface on them.
    /// Call this method at the startup of the application in order to run the migrations.
    /// </para>
    /// </summary>
    public Task MigrateAsync()
        => MigrateAsync(default);

    /// <summary>
    /// Discover and run migrations implementing IMigration or IDatabaseMigration in numeric order.
    /// </summary>
    /// <param name="cancellation">The token used for history operations and database migrations</param>
    /// <remarks>
    /// Legacy IMigration bodies cannot be interrupted by this token. Cancellation does not roll back changes,
    /// and a migration whose completion is not recorded may run again.
    /// </remarks>
    public Task MigrateAsync(CancellationToken cancellation)
        => Migrate(null, cancellation);

    /// <summary>
    /// Executes the given collection of IMigrations in the correct order to transform the database.
    /// </summary>
    /// <param name="migrations">The collection of migrations to execute</param>
    public Task MigrationsAsync(IEnumerable<IMigration> migrations)
        => MigrationsAsync(migrations, default);

    /// <summary>
    /// Executes supplied legacy migrations in numeric order. Their bodies do not receive the token.
    /// </summary>
    /// <param name="migrations">The collection of migrations to execute</param>
    /// <param name="cancellation">The token used for history operations and checks around migration bodies</param>
    public Task MigrationsAsync(IEnumerable<IMigration> migrations, CancellationToken cancellation)
        => Execute(migrations, cancellation);

    /// <summary>
    /// Executes supplied database migrations in numeric order, passing this database instance and the token.
    /// </summary>
    /// <param name="migrations">The collection of migrations to execute</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task MigrationsAsync(IEnumerable<IDatabaseMigration> migrations, CancellationToken cancellation = default)
        => Execute(migrations, cancellation);

    Task Migrate(Type? targetType, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        IEnumerable<Assembly> assemblies;

        if (targetType == null)
        {
            string[] excludes =
            [
                "JetBrains.",
                "Microsoft.",
                "System.",
                "MongoDB.",
                "testhost",
                "netstandard",
                "Newtonsoft.",
                "NuGet",
                "mscorlib",
                "NuGet."
            ];

            assemblies = AppDomain.CurrentDomain
                                  .GetAssemblies()
                                  .Where(a => (!a.IsDynamic && !excludes.Any(n => a.FullName.StartsWith(n))) || a.FullName.StartsWith("MongoDB.Entities.Tests"));
        }
        else
            assemblies = [targetType.Assembly];

        var types = assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters &&
                                (typeof(IMigration).IsAssignableFrom(t) || typeof(IDatabaseMigration).IsAssignableFrom(t)))
                    .ToArray();

        return types.Length == 0
                   ? throw new InvalidOperationException("Didn't find any classes that implement IMigration or IDatabaseMigration.")
                   : Execute(
                       types.Select(
                           t =>
                           {
                               cancellation.ThrowIfCancellationRequested();
                               if (MigrationActivator != null)
                                   return MigrationActivator(t);
                               
                               if (ServiceProvider != null)
                                   return ServiceProvider.GetService(t);

                               return Activator.CreateInstance(t);
                           }), cancellation);
    }

    async Task Execute(IEnumerable<object> migrations, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var lastMigNum = await Find<Migration, int>()
                               .Sort(m => m.Number, Order.Descending)
                               .Project(m => m.Number)
                               .ExecuteFirstAsync(cancellation)
                               .ConfigureAwait(false);

        var dic = new SortedDictionary<int, (string name, object migration)>();

        foreach (var m in migrations)
        {
            cancellation.ThrowIfCancellationRequested();
            if (m is not IMigration && m is not IDatabaseMigration)
                throw new InvalidOperationException("Migration instances must implement IMigration or IDatabaseMigration.");

            var nameParts = m.GetType().Name.Split('_');

            if (nameParts.Length < 3)
                throw new InvalidOperationException("Please use the correct naming format for migration classes!");

            if (!int.TryParse(nameParts[1], out var migNumber))
            {
                throw new InvalidOperationException(
                    "Failed to parse migration number from the class name. Make sure to name the migration classes like: _001_some_migration_name.cs");
            }

            var name = string.Join(" ", nameParts.Skip(2));

            if (migNumber > lastMigNum)
                dic.Add(migNumber, (name, m));
        }

        var sw = new Stopwatch();

        foreach (var migration in dic)
        {
            cancellation.ThrowIfCancellationRequested();
            sw.Start();
            if (migration.Value.migration is IDatabaseMigration databaseMigration)
                await databaseMigration.UpgradeAsync(this, cancellation).ConfigureAwait(false);
            else
                await ((IMigration)migration.Value.migration).UpgradeAsync().ConfigureAwait(false);

            cancellation.ThrowIfCancellationRequested();
            var mig = new Migration(migration.Key, migration.Value.name, sw.Elapsed.TotalSeconds);
            await SaveAsync(mig, cancellation).ConfigureAwait(false);
            sw.Stop();
            sw.Reset();
        }
    }
}
