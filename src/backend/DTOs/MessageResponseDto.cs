using backend.Data.Entities;

namespace backend.DTOs;

public record MessageResponseDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    MessageUserDto Creator,
    IEnumerable<ReactionDto> Reactions
)
{
    public static MessageResponseDto FromEntity(Message message) => new(
        message.Id,
        message.Content,
        message.CreatedAt,
        message.ModifiedAt,
        new MessageUserDto(message.CreatorUser.Id, message.CreatorUser.Username, message.CreatorUser.NameColor),
        message.Reactions.Select(r => new ReactionDto(r.Emoji, new MessageUserDto(r.User.Id, r.User.Username, r.User.NameColor)))
    );
}
