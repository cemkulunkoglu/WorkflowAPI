using System.ComponentModel.DataAnnotations;

namespace MessagesService.Dtos;

public class SendMessageRequest
{
    [Required]
    public int EmployeeToId { get; set; }

    [Required, EmailAddress, MaxLength(255)]
    public string EmailTo { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    public int FlowDesignsId { get; set; }
    public int FlowNodesId { get; set; }
}
