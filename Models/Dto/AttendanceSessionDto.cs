namespace AttendanceWeb.Models.Dto;

public class AttendanceSessionDto
{
    public int CourseId { get; set; }
    public DateTime SessionDate { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public List<ScannedStudentDto> ScannedStudents { get; set; } = new();
}

public class ScannedStudentDto
{
    public string StudentId { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
}
