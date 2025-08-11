using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class CreateMessageDto
{
    [Required]
    [StringLength(200, ErrorMessage = "The content must not exceed 200 characters.")]
    public string content { get; set; } = string.Empty;
}
