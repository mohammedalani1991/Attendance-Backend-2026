namespace AttendanceWeb.Models.Dto;

public class LoginResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public List<CourseDto> Courses { get; set; } = new();
}

public class CourseDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
}
