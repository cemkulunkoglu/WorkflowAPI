using WorkflowManagemetAPI.UoW;
using Microsoft.EntityFrameworkCore;
using WorkflowManagemetAPI.Interfaces.Designs;
using WorkflowManagemetAPI.Models.Designs;

namespace WorkflowManagemetAPI.Repositories.Designs
{
	public class FlowDesignRepository : WorkflowUnitOfWork<FlowDesign>, IFlowDesignRepository
	{
		private readonly Microsoft.EntityFrameworkCore.DbContext context;
		private readonly DbSet<FlowDesign> dbSet;
		public FlowDesignRepository(Microsoft.EntityFrameworkCore.DbContext dbContext) : base(dbContext)
		{
			context = dbContext;
			dbSet = context.Set<FlowDesign>();
		}

		public IEnumerable<FlowDesign> GetAll()
		{
			return dbSet.AsNoTracking().ToList();
		}

        public IEnumerable<FlowDesign> GetDesignByUserId(string userId)
        {
            return dbSet.AsNoTracking()
                .Where(d => d.OwnerUser == userId)
                .ToList();
        }

        public IEnumerable<FlowDesign>? AddRange(List<FlowDesign> flowDesigns)
		{
			InsertRange(flowDesigns);
			SaveChanges();

            return flowDesigns;
		}

        public IEnumerable<FlowDesign>? Add(FlowDesign flowDesign)
        {
            Insert(flowDesign);
            SaveChanges();

            return new List<FlowDesign> { flowDesign };
        }

        public void UpdateDesign(FlowDesign flowDesign)
        {
            Update(flowDesign);
            SaveChanges();
        }

        public void DeleteDesign(FlowDesign flowDesign)
        {
            Delete(flowDesign);
            SaveChanges();
        }
    }
}
