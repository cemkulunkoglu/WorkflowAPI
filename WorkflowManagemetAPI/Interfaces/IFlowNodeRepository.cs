using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
	public interface IFlowNodeRepository : IUnitOfWork<FlowNode>
	{
		IEnumerable<FlowNode> GetByDesignId(int designId);
        void AddRange(List<FlowNode> nodes);

        void DeleteByDesignId(int designId);
    }
}
