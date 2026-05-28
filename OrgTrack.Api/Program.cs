using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Infrastructure.Auth;
using OrgTrack.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"===> RULEZ IN MEDIUL: {builder.Environment.EnvironmentName} <===");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173", "http://localhost:5174") // Adresele standard pentru Vite
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<OrgTrackDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Default"),
            x => x.MigrationsAssembly("OrgTrack.Infrastructure")
        )
    );
}
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero // Token-ul expiră exact la ora setată
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationUnitRepository, OrganizationUnitRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IInviteLinkRepository, InviteLinkRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IGoogleAuthService>(_ =>
    new GoogleAuthService(builder.Configuration["Google:ClientId"]!)
);
builder.Services.AddScoped<ITokenService>(_ =>
    new JwtTokenService(jwtSecret)
);
builder.Services.AddScoped<AuthenticateUser>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<InviteLinkService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrgTrackDbContext>();
    await DataSeeder.SeedDataAsync(dbContext, app.Environment.IsDevelopment());
}
app.UseMiddleware<OrgTrack.Api.Middleware.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Accesibil la /swagger
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
public partial class Program { }
