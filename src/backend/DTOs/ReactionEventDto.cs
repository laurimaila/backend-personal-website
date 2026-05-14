namespace backend.DTOs;

public record ReactionEventDto(int MessageId, string Emoji, bool IsRemoved, MessageUserDto User);
