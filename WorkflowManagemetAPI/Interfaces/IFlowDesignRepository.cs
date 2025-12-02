using WorkflowManagemetAPI.Models;

namespace WorkflowManagemetAPI.Interfaces
{
	public interface IFlowDesignRepository : IUnitOfWork<FlowDesign>
	{
		public IEnumerable<FlowDesign> GetAll();

		public IEnumerable<FlowDesign> GetDesignByUserId(string userId);

        public IEnumerable<FlowDesign>? AddRange(List<FlowDesign> flowDesigns);

		public IEnumerable<FlowDesign>? Add(FlowDesign flowDesign);

		public void UpdateDesign(FlowDesign flowDesign);
		public void DeleteDesign(FlowDesign flowDesign);
    }
}
