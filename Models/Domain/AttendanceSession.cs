using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class AttendanceSession
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [Required]
    public int LecturerId { get; set; }
    public User Lecturer { get; set; } = null!;

    [Required]
    public DateTime SessionDate { get; set; }

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public int TotalScanned { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
