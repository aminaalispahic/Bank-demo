using Microsoft.EntityFrameworkCore;
using BankDemo.Data;
using BankDemo.Interfaces;
using BankDemo.Services;
using BankDemo.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BankDemo.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

// ===== FAZA 1: registruj servise (spisak za kupovinu) =====

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header, format: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });




builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("loginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();
// ^ OVDJE se "spisak za kupovinu" pretvara u stvarnu, funkcionalnu aplikaciju.
//   Poslije ove linije, VIŠE NE MOŽEŠ dodavati servise sa builder.Services

// ===== FAZA 2: middleware pipeline (traka za obradu zahtjeva, REDOSLIJED BITAN) =====

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
});

app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthentication();
// ^ provjerava KO si (čita i validira JWT token) — MORA biti prije UseAuthorization

app.UseAuthorization();
// ^ provjeri da li korisnik ima dozvolu — MORA biti prije MapControllers




app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    bool adminExists = context.Users.Any(u => u.Role == BankDemo.Enums.UserRole.Admin);

    if (!adminExists)
    {
        var adminUsername = builder.Configuration["AdminSeed:Username"];
        var adminPassword = builder.Configuration["AdminSeed:Password"];

        if (!string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword))
        {
            context.Users.Add(new BankDemo.Models.User
            {
                Username = adminUsername,
                Email = "admin@bankdemo.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = BankDemo.Enums.UserRole.Admin
            });

            context.SaveChanges();
        }
    }
}
app.Run();