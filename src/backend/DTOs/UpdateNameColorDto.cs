using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class UpdateNameColorDto
{
    [Required]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (e.g. #ff0000)")]
    public string Color { get; set; } = string.Empty;
}
