using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.Domain;

public class UnpaidStudent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public DateTime MarkedUnpaidAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
