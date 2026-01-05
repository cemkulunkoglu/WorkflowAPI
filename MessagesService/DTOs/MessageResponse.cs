namespace MessagesService.Dtos;

public class MessageResponse
{
    public int Id { get; set; }
    public int FlowDesignsId { get; set; }
    public int FlowNodesId { get; set; }
    public int EmployeeToId { get; set; }
    public int EmployeeFromId { get; set; }
    public string EmailTo { get; set; } = string.Empty;
    public string EmailFrom { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}
