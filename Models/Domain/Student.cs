using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class Student
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string StudentId { get; set; } = string.Empty; // Numeric barcode value

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public int StageId { get; set; }
    public Stage Stage { get; set; } = null!;

    [Required]
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
