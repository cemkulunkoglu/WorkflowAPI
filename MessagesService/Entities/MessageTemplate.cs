using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities;

[Table("MessageTemplates")]
public class MessageTemplate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TemplateId { get; set; }

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }

    public bool IsActive { get; set; } = true;

    public int? UserId { get; set; }

    public ICollection<MessageTemplateField> Fields { get; set; } = new List<MessageTemplateField>();
}
