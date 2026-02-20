using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.ViewModels;

public class EditDepartmentViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Department Name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Department Code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "New Password (leave blank to keep current)")]
    [MinLength(6)]
    public string? NewPassword { get; set; }
}
