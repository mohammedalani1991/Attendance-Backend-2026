using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
}
