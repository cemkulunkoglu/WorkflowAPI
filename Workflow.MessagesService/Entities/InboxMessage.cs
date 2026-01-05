using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Workflow.MessagesService.Entities;

public class InboxMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int FlowDesignsId { get; set; }

    public int FlowNodesId { get; set; }

    public int EmployeeToId { get; set; }

    public int EmployeeFromId { get; set; }

    [Column(TypeName = "varchar(255)")]
    public string EmailTo { get; set; } = string.Empty;

    [Column(TypeName = "varchar(255)")]
    public string EmailFrom { get; set; } = string.Empty;

    [Column(TypeName = "varchar(255)")]
    public string Subject { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime CreateDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdateDate { get; set; }
}
