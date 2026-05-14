using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class ReactToMessageDto
{
    [Required]
    [MaxLength(32)]
    public string Emoji { get; set; } = string.Empty;
}
