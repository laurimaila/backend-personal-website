using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class MessageDto
{
    [Required]
    [StringLength(200, ErrorMessage = "The content must not exceed 200 characters.")]
    public string content { get; set; } = string.Empty;

    [Required]
    [StringLength(30, ErrorMessage = "The name must not exceed 30 characters.")]
    public string creator { get; set; } = string.Empty;
}
