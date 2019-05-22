using Microsoft.EntityFrameworkCore;

namespace ShortURL.Data
{
    public class Context : DbContext
    {
        public Context(DbContextOptions options) : base(options) { }

        public void Migrate() => Database.Migrate();

        public System.Collections.Generic.IEnumerable<string> GetPendingMigrations() => Database.GetPendingMigrations();

        public DbSet<Model.Domain> Domains { get; set; }
        public DbSet<Model.Group> Groups { get; set; }
        public DbSet<Model.GroupVisit> GroupVisits { get; set; }
        public DbSet<Model.Record> Records { get; set; }
        public DbSet<Model.RecordVisit> RecordVisits { get; set; }
    }
}
