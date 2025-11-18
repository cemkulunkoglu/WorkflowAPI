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
                eb.OwnsOne(fn => fn.LabelStyle);
                eb.OwnsOne(fn => fn.Style);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}