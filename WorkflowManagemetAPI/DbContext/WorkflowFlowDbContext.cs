using Microsoft.EntityFrameworkCore;
using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.DbContext
{
    public class WorkflowFlowDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public WorkflowFlowDbContext(DbContextOptions<WorkflowFlowDbContext> options) : base(options) { }

        public DbSet<FlowDesign> FlowDesigns { get; set; }
        public DbSet<FlowNode> FlowNodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            base.OnModelCreating(modelBuilder);
        }
    }
}