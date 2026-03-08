using backend.Common;
using backend.Configuration;
using backend.Extensions;
using backend.Middleware;
using backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiExceptionAuthorizationHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var configOrigins = builder.Configuration
            .GetSection("CorsOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        var appSettings = builder.Configuration.Get<ApplicationSettings>();

        var allowedOrigins = configOrigins;

        if (appSettings != null && !string.IsNullOrWhiteSpace(appSettings.CorsOrigins))
        {
            var parsedAppOrigins = appSettings.CorsOrigins
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            allowedOrigins = configOrigins.Concat(parsedAppOrigins).ToArray();
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

HttpLogger.ConfigureHttpLogging(builder.Services, builder.Environment.IsDevelopment());

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

try
{
    using var scope = app.Services.CreateScope();
    var databaseInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitService>();
    await databaseInitService.InitializeAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Failed to initialize database");
}

app.UseExceptionHandlerMiddleware();

app.UseHttpLogging();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();

app.UseAuthenticationMiddleware();
app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var settings = context.RequestServices.GetRequiredService<IOptions<ApplicationSettings>>().Value;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            environment = app.Environment.EnvironmentName,
            version = settings.Version,
            timestamp = DateTime.UtcNow
        });
    }
});

app.Run();
