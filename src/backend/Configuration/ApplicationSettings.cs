using System.ComponentModel.DataAnnotations;

namespace backend.Configuration;

public class ApplicationSettings
{
    [Required]
    [ConfigurationKeyName("POSTGRES_USER")]
    public string PostgresUser { get; set; } = string.Empty;

    [Required]
    [ConfigurationKeyName("POSTGRES_PASSWORD")]
    public string PostgresPassword { get; set; } = string.Empty;

    [Required]
    [ConfigurationKeyName("POSTGRES_DB")]
    public string PostgresDb { get; set; } = string.Empty;

    [Required]
    [ConfigurationKeyName("POSTGRES_HOST")]
    public string PostgresHost { get; set; } = string.Empty;

    [Required]
    [MinLength(16)]
    [ConfigurationKeyName("JWT_SECRET_KEY")]
    public string JwtSecretKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [ConfigurationKeyName("JWT_EXPIRY_HOURS")]
    public int JwtExpiryHours { get; set; } = 24;

    [Required]
    [Url]
    [ConfigurationKeyName("PHYSICS_SVC_URL")]
    public string PhysicsSvcUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("CONNECTION_STRING")]
    public string? ConnectionString { get; set; }

    [ConfigurationKeyName("CORS_ORIGINS")]
    public string? CorsOrigins { get; set; }

    [ConfigurationKeyName("BACKEND_VERSION")]
    public string Version { get; set; } = "unknown";
}
