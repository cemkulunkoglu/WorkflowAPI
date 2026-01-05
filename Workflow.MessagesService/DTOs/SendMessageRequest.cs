using System.ComponentModel.DataAnnotations;

namespace Workflow.MessagesService.DTOs;

public class SendMessageRequest
{
    [Required]
    public int FlowDesignsId { get; set; }

    [Required]
    public int FlowNodesId { get; set; }

    [Required]
    public int EmployeeToId { get; set; }

    [Required]
    public int EmployeeFromId { get; set; }

    [Required]
    public string EmailTo { get; set; } = string.Empty;

    [Required]
    public string EmailFrom { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;
}
