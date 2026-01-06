using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowManagemetAPI.Models.Employees
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        // AuthServer'daki kullanıcı ID
        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? SicilNo { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        public int? ManagerId { get; set; }

        [MaxLength(255)]
        public string? Path { get; set; }

        // Self relation (opsiyonel)
        [ForeignKey(nameof(ManagerId))]
        public Employee? Manager { get; set; }

        public ICollection<Employee> Children { get; set; } = new List<Employee>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
