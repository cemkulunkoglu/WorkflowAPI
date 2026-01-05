using WorkflowManagemetAPI.DTOs;

public interface IDesignService
{
    FlowDesignDto GetFlowDesignById(int designId);
    IEnumerable<FlowDesignDto> GetFlowDesignsByUserId();
    IEnumerable<FlowDesignDto> GetAllFlowDesigns();
    FlowDesignDto CreateFlowDesign(CreateFlowDesignRequest request);
    FlowDesignDto UpdateFlowDesign(int id, CreateFlowDesignRequest request);
    void DeleteFlowDesign(int designId);
}
