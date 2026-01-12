using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities;

[Table("MessageTemplateFields")]
public class MessageTemplateField
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FieldId { get; set; }

    public int TemplateId { get; set; }

    [MaxLength(255)]
    public string? DynamicField { get; set; }

    [MaxLength(255)]
    public string SystemField { get; set; } = string.Empty;

    [ForeignKey(nameof(TemplateId))]
    public MessageTemplate? Template { get; set; }
}
