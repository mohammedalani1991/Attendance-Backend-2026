namespace AttendanceWeb.Models.Dto;

public class StudentDto
{
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsUnpaid { get; set; }
}
