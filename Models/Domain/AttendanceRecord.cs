using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class AttendanceRecord
{
    public int Id { get; set; }

    [Required]
    public int AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string StudentId { get; set; } = string.Empty;

    public DateTime ScannedAt { get; set; }

    public bool IsPresent { get; set; } // true for scanned, false for absent

    public bool IsUnpaid { get; set; } // flagged if in unpaid list
}
