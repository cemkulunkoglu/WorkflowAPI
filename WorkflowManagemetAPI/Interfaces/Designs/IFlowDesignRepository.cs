using WorkflowManagemetAPI.Models.Designs;

namespace WorkflowManagemetAPI.Interfaces.Designs
{
	public interface IFlowDesignRepository : IWorkflowUnitOfWork<FlowDesign>
	{
		public IEnumerable<FlowDesign> GetAll();

		public IEnumerable<FlowDesign> GetDesignByUserId(string userId);

        public IEnumerable<FlowDesign>? AddRange(List<FlowDesign> flowDesigns);

		public IEnumerable<FlowDesign>? Add(FlowDesign flowDesign);

		public void UpdateDesign(FlowDesign flowDesign);
		public void DeleteDesign(FlowDesign flowDesign);
    }
}
