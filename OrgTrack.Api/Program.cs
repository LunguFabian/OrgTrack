using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrgTrack.Domain.Interfaces;
using OrgTrack.Application.Interfaces;
using OrgTrack.Application.UseCases;
using OrgTrack.Infrastructure.Auth;
using OrgTrack.Infrastructure.Persistence;
using OrgTrack.Infrastructure.Repositories;
using OrgTrack.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"===> RULEZ IN MEDIUL: {builder.Environment.EnvironmentName} <===");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000", "http://localhost", "http://165.232.79.84:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // Fail instantly when limit is reached
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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

        // Allow JWT token from query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
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
    new GoogleAuthService(builder.Configuration["Google:ClientId"]!, builder.Configuration["Google:ClientSecret"]!)
);
builder.Services.AddScoped<ITokenService>(_ =>
    new JwtTokenService(jwtSecret)
);
builder.Services.AddScoped<AuthenticateUser>();
builder.Services.AddScoped<ConnectGoogleCalendar>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<InviteLinkService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IGoogleCalendarService, OrgTrack.Infrastructure.ExternalServices.GoogleCalendarService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IEmailService, OrgTrack.Infrastructure.ExternalServices.SmtpEmailService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, OrgTrack.Api.Hubs.SignalRNotifier>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

var app = builder.Build();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrgTrackDbContext>();
    // Apply pending migrations automatically on startup
    await dbContext.Database.MigrateAsync();
    
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrgTrackHub>("/hubs/orgtrack");
app.MapHealthChecks("/health");

await app.RunAsync();
public partial class Program { }
