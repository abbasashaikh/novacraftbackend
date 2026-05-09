using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NovaCraft.Data;
using NovaCraft.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// SQLite
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();

// CORS — allow React frontend
builder.Services.AddCors(opt => opt.AddPolicy("Frontend", policy =>
    /* policy.WithOrigins(
        builder.Configuration["Frontend:Url"] ?? "http://localhost:3151",
        "https://novacraft-frontend-ffi9g2vpr-abbas-projects-c2fffa0a.vercel.app",
        "https://novacraft-frontend.vercel.app/",
        "http://localhost:3151",
        "http://localhost:5173",
        "http://localhost:3000"
    ), */
    policy.SetIsOriginAllowed(origin =>
        {
            // Allow production domain
            if (origin == "https://novacraft-frontend.vercel.app") return true;
            
            // Allow all Vercel preview deployments
            if (origin.StartsWith("https://novacraft-frontend-") && 
                origin.EndsWith(".vercel.app")) return true;
            
            // Allow localhost for development
            if (origin.StartsWith("http://localhost:")) return true;
            
            return false;
        })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
));

var app = builder.Build();

// ── Migrate DB on startup ─────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => new { status = "ok", app = "NovaCraft API", version = "1.0" });

Console.WriteLine("✅ NovaCraft API running");
Console.WriteLine("   Endpoints:");
Console.WriteLine("   POST /api/auth/register");
Console.WriteLine("   POST /api/auth/login");
Console.WriteLine("   GET  /api/subscription/status  ← APK/EXE check access here");
Console.WriteLine("   GET  /health");

app.Run();
