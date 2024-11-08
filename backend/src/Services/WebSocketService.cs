using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using backend.Models;
using backend.DTOs;

namespace backend.Services;

public interface IWebSocketService
{
    Task HandleWebSocketConnection(WebSocket webSocket);
    Task BroadcastMessage(object message);
    Task SendToClient(WebSocket socket, string type, object payload);
}

public class WebSocketService(
    ILogger<WebSocketService> logger,
    IMessageService messageService,
    IValidationService validationService) : IWebSocketService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly List<WebSocket> Clients = [];

    public async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        Clients.Add(webSocket);
        logger.LogInformation("New client connected. Total clients: {Count}", Clients.Count);
        try
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var received = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (received.MessageType == WebSocketMessageType.Close)
                    break;

                if (received.MessageType != WebSocketMessageType.Text)
                    continue;

                var messageJson = Encoding.UTF8.GetString(buffer, 0, received.Count);
                var incomingMessage = JsonSerializer.Deserialize<MessageDto>(messageJson);
                if (incomingMessage is null)
                {
                    await SendToClient(webSocket, WebSocketMessageTypes.Error, new ErrorDto
                    {
                        Code = "INVALID_MESSAGE",
                        Message = "Invalid message format"
                    });
                    continue;
                }

                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(incomingMessage, new ValidationContext(incomingMessage),
                        validationResults, true))
                {
                    var errors = validationResults.Select(x => x.ErrorMessage).ToList();
                    await SendToClient(webSocket, WebSocketMessageTypes.Error, new ErrorDto
                    {
                        Code = "VALIDATION_ERROR",
                        Message = string.Join(", ", errors)
                    });
                    continue;
                }

                var savedMessage = await messageService.CreateMessageAsync(incomingMessage);
                await BroadcastMessage(savedMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling WebSocket connection");
        }
        finally
        {
            Clients.Remove(webSocket);
            logger.LogInformation("Client disconnected. Total clients: {Count}", Clients.Count);
        }
    }

    public async Task SendToClient(WebSocket socket, string type, object payload)
    {
        try
        {
            var message = new WebSocketMessage<object>
            {
                Type = type,
                Payload = payload
            };

            var json = JsonSerializer.Serialize(message, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to client");
        }
    }

    public async Task BroadcastMessage(object message)
    {
        var deadSockets = new List<WebSocket>();

        foreach (var client in Clients)
        {
            try
            {
                await SendToClient(client, WebSocketMessageTypes.Message, message);
            }
            catch (Exception)
            {
                deadSockets.Add(client);
            }
        }

        // Clean up dead connections
        foreach (var socket in deadSockets)
        {
            Clients.Remove(socket);
        }
    }
}
