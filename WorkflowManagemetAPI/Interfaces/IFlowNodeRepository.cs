using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
	public interface IFlowNodeRepository : IWorkflowUnitOfWork<FlowNode>
	{
		IEnumerable<FlowNode> GetByDesignId(int designId);
        void AddRange(List<FlowNode> nodes);
        FlowNode? Add(FlowNode flowNode);
        void DeleteByDesignId(int designId);

    }
}
