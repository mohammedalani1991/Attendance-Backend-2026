using System.ComponentModel.DataAnnotations;

namespace AttendanceWeb.Models.ViewModels;

public class CreateDepartmentViewModel
{
    [Required]
    [Display(Name = "Department Name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Department Code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Username")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
