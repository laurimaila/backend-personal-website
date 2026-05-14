using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Data.Entities;

[Table("message_reactions")]
public class MessageReaction
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("message_id")]
    public int MessageId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("emoji")]
    [MaxLength(32)]
    public string Emoji { get; set; } = string.Empty;

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
