using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities;

[Table("Outbox")]
public class OutboxMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FlowDesignsId { get; set; }
    public int FlowNodesId { get; set; }

    public int EmployeeToId { get; set; }
    public int EmployeeFromId { get; set; }

    [MaxLength(255)]
    public string EmailTo { get; set; } = string.Empty;

    [MaxLength(255)]
    public string EmailFrom { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public int RetryCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
}
