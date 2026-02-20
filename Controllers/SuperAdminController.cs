using AttendanceWeb.Data;
using AttendanceWeb.Models.Domain;
using AttendanceWeb.Models.ViewModels;
using AttendanceWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public SuperAdminController(ApplicationDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();

        return View(departments);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDepartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _context.Departments.AnyAsync(d => d.Code == model.Code))
        {
            ModelState.AddModelError("Code", "Department code already exists.");
            return View(model);
        }

        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError("Username", "Username already exists.");
            return View(model);
        }

        var department = new Department
        {
            Name = model.Name,
            Code = model.Code,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        var departmentUser = await _authService.CreateUser(
            model.Username,
            model.Password,
            UserRole.DepartmentUser,
            department.Id
        );

        TempData["SuccessMessage"] = $"Department '{department.Name}' created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        var departmentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.DepartmentId == id && u.Role == UserRole.DepartmentUser);

        var model = new EditDepartmentViewModel
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            Username = departmentUser?.Username ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditDepartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var department = await _context.Departments.FindAsync(model.Id);
        if (department == null)
        {
            return NotFound();
        }

        if (await _context.Departments.AnyAsync(d => d.Code == model.Code && d.Id != model.Id))
        {
            ModelState.AddModelError("Code", "Department code already exists.");
            return View(model);
        }

        department.Name = model.Name;
        department.Code = model.Code;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var departmentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.DepartmentId == model.Id && u.Role == UserRole.DepartmentUser);

            if (departmentUser != null)
            {
                departmentUser.PasswordHash = _authService.HashPassword(model.NewPassword);
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Department '{department.Name}' updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Stages)
            .Include(d => d.Students)
            .Include(d => d.Users)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        // Check if department has related data
        var stageCount = department.Stages.Count;
        var studentCount = department.Students.Count;
        var userCount = department.Users.Count;

        if (stageCount > 0 || studentCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete department '{department.Name}'. " +
                $"It has {stageCount} stage(s) and {studentCount} student(s). " +
                "Please delete all stages and students first.";
            return RedirectToAction(nameof(Index));
        }

        // Delete associated users (department users and lecturers)
        if (userCount > 0)
        {
            _context.Users.RemoveRange(department.Users);
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Department '{department.Name}' deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}
