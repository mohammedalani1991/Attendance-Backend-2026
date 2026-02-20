using AttendanceWeb.Data;
using AttendanceWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use Railway's PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT");
if (port != null)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
// Use SQL Server in production (MonsterASP.NET), SQLite in development
var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(sqlServerConnection))
        options.UseSqlServer(sqlServerConnection);
    else
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExcelService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddControllersWithViews();

// Add CORS for mobile app
// Supports ALLOWED_ORIGINS env var for Railway (comma-separated)
var allowedOrigins = new List<string>
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5173",
    "http://127.0.0.1:5174",
    "capacitor://localhost",
    "http://localhost",
    "https://localhost"
};

var extraOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
if (!string.IsNullOrEmpty(extraOrigins))
{
    allowedOrigins.AddRange(extraOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Railway handles SSL termination at the proxy level — skip HTTPS redirection
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// Enable CORS
app.UseCors("AllowMobileApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
