using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
	public interface IFlowNodeRepository : IUnitOfWork<FlowNode>
	{
		public IEnumerable<FlowNode> GetByDesignId(int designId);
	}
}
