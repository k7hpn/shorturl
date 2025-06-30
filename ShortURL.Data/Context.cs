using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ShortURL.Data
{
    public class Context(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Model.Domain> Domains { get; set; }

        public DbSet<Model.Group> Groups { get; set; }

        public DbSet<Model.GroupVisit> GroupVisits { get; set; }

        public DbSet<Model.Record> Records { get; set; }

        public DbSet<Model.RecordVisit> RecordVisits { get; set; }

        public string GetCurrentMigration() => Database.GetAppliedMigrations().Last();

        public IEnumerable<string> GetPendingMigrationList() => Database.GetPendingMigrations();

        public void Migrate() => Database.Migrate();
    }
}