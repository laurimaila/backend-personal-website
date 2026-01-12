namespace backend.Models;

public class WebSocketMessage<T>
{
    public string Type { get; set; } = string.Empty;
    public T Payload { get; set; } = default!;
}

public static class WebSocketMessageTypes
{
    public const string Message = "message";
    public const string Error = "error";
    public const string Status = "status";
}
