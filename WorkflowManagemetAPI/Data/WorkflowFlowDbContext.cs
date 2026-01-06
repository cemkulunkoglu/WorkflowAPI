using Microsoft.EntityFrameworkCore;
using WorkflowManagemetAPI.Models.Designs;
using WorkflowManagemetAPI.Models.Employees;

namespace WorkflowManagemetAPI.DbContext
{
    public class WorkflowFlowDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public WorkflowFlowDbContext(DbContextOptions<WorkflowFlowDbContext> options)
            : base(options)
        {
        }

        public DbSet<FlowDesign> FlowDesigns { get; set; }
        public DbSet<FlowNode> FlowNodes { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ 1) FlowNode -> Owned Types (LabelStyle, Style)
            modelBuilder.Entity<FlowNode>(eb =>
            {
                eb.OwnsOne(fn => fn.LabelStyle, navigation =>
                {
                    navigation.WithOwner();
                });
                eb.Navigation(fn => fn.LabelStyle).IsRequired(false);

                eb.OwnsOne(fn => fn.Style, navigation =>
                {
                    navigation.WithOwner();
                });
                eb.Navigation(fn => fn.Style).IsRequired(false);
            });

            // ✅ 2) Employee mapping
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

            base.OnModelCreating(modelBuilder);
        }

    }
}
