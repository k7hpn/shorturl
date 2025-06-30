using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ShortURL
{
    internal static class DatabaseMigratorExtensions
    {
        /// <summary>
        /// Run any pending database migrations
        /// </summary>
        /// <param name="applicationBuilder">The ApplicationBuilder context providing services for
        /// ILogger<DatabaseMigrator> and ApplicationDbContext.</DatabaseMigrator></param>
        /// <returns>The passed-in ApplicationBuilder</returns>
        internal static IApplicationBuilder
            RunMigrations(this IApplicationBuilder applicationBuilder)
        {
            new DatabaseMigrator(applicationBuilder).RunMigrations();
            return applicationBuilder;
        }
    }

    internal class DatabaseMigrator(IApplicationBuilder applicationBuilder)
    {
        private readonly IApplicationBuilder _applicationBuilder = applicationBuilder
            ?? throw new ArgumentNullException(nameof(applicationBuilder));

        internal void RunMigrations()
        {
            using var scope = _applicationBuilder.ApplicationServices.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrator>>();

            using var context = scope.ServiceProvider.GetRequiredService<Data.Context>();
            bool hasMigrations;
            try
            {
                var pending = context.GetPendingMigrationList();
                hasMigrations = pending?.Any() == true;
                if (hasMigrations)
                {
                    logger.LogWarning(
                        "Applying {MigrationsCount} database migrations, last is: {LastMigration}",
                        pending?.Count(),
                        pending?.Last());
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "Unable to determine how many migrations are pending: {ErrorMessage}",
                    ex.Message);
                throw;
            }

            try
            {
                if (hasMigrations)
                {
                    var timer = Stopwatch.StartNew();
                    context.Migrate();
                    logger.LogWarning("Migrations complete in {Elapsed} ms",
                        timer.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "Critical error performing migrations: {ErrorMessage}",
                    ex.Message);
                throw;
            }
        }
    }
}