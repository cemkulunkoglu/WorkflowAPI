using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities;

[Table("ProcessedMessages")]
public class ProcessedMessage
{
    [Key]
    [MaxLength(64)]
    public string MessageId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EventName { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Processed";

    public string? LastError { get; set; }
}
