namespace WorkflowManagemetAPI.Models.Designs
{
    public class FlowDesign
    {
        public int Id { get; set; }
        public string DesignName { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public string? OwnerUser { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }
}