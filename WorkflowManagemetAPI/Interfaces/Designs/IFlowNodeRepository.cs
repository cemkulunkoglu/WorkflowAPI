using WorkflowManagemetAPI.Models.Designs;

namespace WorkflowManagemetAPI.Interfaces.Designs
{
	public interface IFlowNodeRepository : IWorkflowUnitOfWork<FlowNode>
	{
		IEnumerable<FlowNode> GetByDesignId(int designId);
        void AddRange(List<FlowNode> nodes);
        FlowNode? Add(FlowNode flowNode);
        void DeleteByDesignId(int designId);

    }
}
