using Microsoft.EntityFrameworkCore;
using Wakecap.Models;

namespace Wakecap.Data
{
    public class ApplicationDbContext: DbContext
    {
        public DbSet<Worker> Workers { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<WorkerZoneAssignment> WorkerZoneAssignments { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<WorkerZoneAssignment>()
                .HasOne(wza => wza.Worker)
                .WithMany(w => w.WorkerZoneAssignments)
                .HasForeignKey(wza => wza.WorkerId)
                .OnDelete(DeleteBehavior.Cascade); // Configure delete behavior

            modelBuilder.Entity<WorkerZoneAssignment>()
               .HasOne(wza => wza.Zone)
               .WithMany(z => z.WorkerZoneAssignments)
               .HasForeignKey(wza => wza.ZoneId)
               .OnDelete(DeleteBehavior.Cascade); // Configure delete behavior
        }
    }
}
