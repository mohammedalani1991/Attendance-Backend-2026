using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class Course
{
    public int Id { get; set; }

    [Required]
    public int StageId { get; set; }
    public Stage Stage { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public int? LecturerId { get; set; }
    public User? Lecturer { get; set; }

    // Navigation properties
    public ICollection<AttendanceSession> AttendanceSessions { get; set; } = new List<AttendanceSession>();
}
