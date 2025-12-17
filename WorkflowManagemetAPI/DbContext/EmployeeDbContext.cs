using Microsoft.EntityFrameworkCore;
using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.DbContext
{
    public class EmployeeDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employee");
                entity.HasKey(e => e.EmployeeId);

                entity.Property(e => e.Path).HasMaxLength(255);

                entity.HasOne(e => e.Manager)
                      .WithMany(m => m.Children)
                      .HasForeignKey(e => e.ManagerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
