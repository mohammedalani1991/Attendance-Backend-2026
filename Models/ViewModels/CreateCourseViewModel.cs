using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.ViewModels;

public class CreateCourseViewModel
{
    public int StageId { get; set; }
    public string StageName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Course Name")]
    [MaxLength(200)]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Course Code")]
    [MaxLength(50)]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Lecturer Username")]
    [MaxLength(100)]
    public string LecturerUsername { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Lecturer Password")]
    [MinLength(6)]
    public string LecturerPassword { get; set; } = string.Empty;
}
