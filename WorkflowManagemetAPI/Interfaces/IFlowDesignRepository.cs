using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
	public interface IFlowDesignRepository : IUnitOfWork<FlowDesign>
	{
		public IEnumerable<FlowDesign> GetAll();
	}
}
