namespace WorkflowManagemetAPI.Interfaces
{
    using WorkflowManagemetAPI.DTOs;

    public interface IDesignService
    {
        FlowDesignDto GetFlowDesignById(int designId);

        IEnumerable<FlowDesignDto> GetAllFlowDesigns();

        FlowDesignDto CreateFlowDesign(CreateFlowDesignRequest request);
    }
}
