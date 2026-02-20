using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class Department
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Stage> Stages { get; set; } = new List<Stage>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<UnpaidStudent> UnpaidStudents { get; set; } = new List<UnpaidStudent>();
}
