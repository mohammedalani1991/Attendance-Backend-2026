using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class Stage
{
    public int Id { get; set; }

    [Required]
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // "Stage 1", "Stage 2", etc.

    [Required]
    public int Year { get; set; } // 1, 2, 3, 4

    // Navigation properties
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
