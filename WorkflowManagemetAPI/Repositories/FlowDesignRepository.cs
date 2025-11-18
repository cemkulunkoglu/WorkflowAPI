using WorkflowManagemetAPI.Models;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.UoW;
using Microsoft.EntityFrameworkCore;

namespace WorkflowManagemetAPI.Repositories
{
	public class FlowDesignRepository : UnitOfWork<FlowDesign>, IFlowDesignRepository
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
	}
}
