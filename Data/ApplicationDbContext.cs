using AttendanceWeb.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Stage> Stages { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<UnpaidStudent> UnpaidStudents { get; set; }
    public DbSet<AttendanceSession> AttendanceSessions { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<ApiToken> ApiTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Role).HasConversion<int>();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Department configurations
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Stage configurations
        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Stages)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Course configurations
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Stage)
                .WithMany(s => s.Courses)
                .HasForeignKey(e => e.StageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lecturer)
                .WithMany()
                .HasForeignKey(e => e.LecturerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Student configurations
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentId).IsUnique();

            entity.HasOne(e => e.Stage)
                .WithMany(s => s.Students)
                .HasForeignKey(e => e.StageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Students)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UnpaidStudent configurations
        modelBuilder.Entity<UnpaidStudent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StudentId, e.DepartmentId }).IsUnique();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.UnpaidStudents)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AttendanceSession configurations
        modelBuilder.Entity<AttendanceSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Course)
                .WithMany(c => c.AttendanceSessions)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lecturer)
                .WithMany(u => u.AttendanceSessions)
                .HasForeignKey(e => e.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AttendanceRecord configurations
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AttendanceSessionId, e.StudentId });

            entity.HasOne(e => e.AttendanceSession)
                .WithMany(a => a.AttendanceRecords)
                .HasForeignKey(e => e.AttendanceSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiToken configurations
        modelBuilder.Entity<ApiToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Super Admin user
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Create a default Super Admin user
        // Username: admin, Password: admin
        var superAdmin = new User
        {
            Id = 1,
            Username = "admin",
            // BCrypt hash for "admin"
            PasswordHash = "$2a$11$MsFRQdgIUvHi5OYhHZ.kr.kwPnopjA6pMjpoGcGesOYfZW51/gunC",
            Role = UserRole.SuperAdmin,
            DepartmentId = null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        modelBuilder.Entity<User>().HasData(superAdmin);
    }
}
