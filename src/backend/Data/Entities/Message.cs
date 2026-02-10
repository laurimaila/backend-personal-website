using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Data.Entities;

[Table("messages")]
public class Message
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("content")]
    [MaxLength(255)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Column("creator")]
    [MaxLength(50)]
    public string Creator { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }
}
