using AttendanceWeb.Data;
using AttendanceWeb.Models.Domain;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace AttendanceWeb.Services;

public class ExcelService
{
    private readonly ApplicationDbContext _context;

    static ExcelService()
    {
        ExcelPackage.License.SetNonCommercialPersonal("AttendanceSystem");
    }

    public ExcelService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Student>> ParseStudentExcel(Stream fileStream, int departmentId)
    {
        var students = new List<Student>();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets[0];

        if (worksheet == null)
        {
            throw new Exception("Excel file is empty or invalid.");
        }

        int rowCount = worksheet.Dimension?.Rows ?? 0;

        if (rowCount < 2)
        {
            throw new Exception("Excel file must contain at least a header row and one data row.");
        }

        // Get department stages
        var stages = await _context.Stages
            .Where(s => s.DepartmentId == departmentId)
            .ToListAsync();

        if (!stages.Any())
        {
            throw new Exception("Please create stages before uploading students.");
        }

        // Expected columns: StudentId, FullName, Stage (year number)
        for (int row = 2; row <= rowCount; row++)
        {
            var studentId = worksheet.Cells[row, 1].Text?.Trim();
            var fullName = worksheet.Cells[row, 2].Text?.Trim();
            var stageYearText = worksheet.Cells[row, 3].Text?.Trim();

            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(stageYearText))
            {
                throw new Exception($"Row {row}: All fields (StudentId, FullName, Stage) are required.");
            }


            // Check if student ID already exists
            if (await _context.Students.AnyAsync(s => s.StudentId == studentId))
            {
                throw new Exception($"Row {row}: Student ID '{studentId}' already exists in the database.");
            }

            // Parse stage year
            if (!int.TryParse(stageYearText, out int stageYear))
            {
                throw new Exception($"Row {row}: Stage must be a number (1, 2, 3, or 4).");
            }

            var stage = stages.FirstOrDefault(s => s.Year == stageYear);
            if (stage == null)
            {
                throw new Exception($"Row {row}: Stage {stageYear} does not exist for this department. Please create it first.");
            }

            students.Add(new Student
            {
                StudentId = studentId,
                FullName = fullName,
                StageId = stage.Id,
                DepartmentId = departmentId,
                CreatedAt = DateTime.UtcNow
            });
        }

        return students;
    }

    public async Task<List<string>> ParseUnpaidExcel(Stream fileStream, int departmentId)
    {
        var studentIds = new List<string>();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets[0];

        if (worksheet == null)
        {
            throw new Exception("Excel file is empty or invalid.");
        }

        int rowCount = worksheet.Dimension?.Rows ?? 0;

        if (rowCount < 2)
        {
            throw new Exception("Excel file must contain at least a header row and one data row.");
        }

        // Expected column: StudentId
        for (int row = 2; row <= rowCount; row++)
        {
            var studentId = worksheet.Cells[row, 1].Text?.Trim();

            if (string.IsNullOrWhiteSpace(studentId))
            {
                continue; // Skip empty rows
            }


            // Verify student exists
            if (!await _context.Students.AnyAsync(s => s.StudentId == studentId && s.DepartmentId == departmentId))
            {
                throw new Exception($"Row {row}: Student ID '{studentId}' not found in this department.");
            }

            studentIds.Add(studentId);
        }

        return studentIds.Distinct().ToList();
    }
}
