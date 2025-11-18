using Dapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Linq;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Models;
using WorkflowManagemetAPI.UoW;

namespace WorkflowManagemetAPI.Repositories
{
    public class FlowNodeRepository : UnitOfWork<FlowNode>, IFlowNodeRepository
	{
		private readonly Microsoft.EntityFrameworkCore.DbContext _dbContext;
		private readonly DbSet<FlowNode> dbSet;
		public FlowNodeRepository(Microsoft.EntityFrameworkCore.DbContext dbContext) 
            : base(dbContext) // Pass dbContext to base UnitOfWork<FlowNode> constructor
        {
            _dbContext = dbContext;
            dbSet = _dbContext.Set<FlowNode>();
        }

		public IEnumerable<FlowNode> GetByDesignId(int groupId)
		{
			return dbSet.Where(x => x.DesignId == groupId).AsNoTracking().ToList();
		}
		public IEnumerable<FlowNode> GetAll()
		{
			return dbSet.AsNoTracking().ToList();
		}
	}
}
