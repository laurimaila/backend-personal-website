using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using backend.Data.Entities;
using backend.DTOs;
using backend.Middleware;
using backend.Models;

namespace backend.Services;

public interface IWebSocketService
{
    Task HandleWebSocketConnection(WebSocket webSocket, HttpContext httpContext);
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

    private static readonly ConcurrentDictionary<WebSocket, byte> Clients = new();

    public async Task HandleWebSocketConnection(WebSocket webSocket, HttpContext httpContext)
    {
        // Get authenticated user from the HTTP context set by middleware
        var authenticatedUser = httpContext.Items["User"] as User;

        if (authenticatedUser == null)
        {
            logger.LogWarning("Unauthorized WebSocket connection attempt");
            await webSocket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Authentication required",
                CancellationToken.None);
            return;
        }

        Clients.TryAdd(webSocket, 0);
        logger.LogInformation("Authenticated WebSocket client connected: {Username}. Total clients: {Count}",
            authenticatedUser.Username, Clients.Count);

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
                var incomingMessage = JsonSerializer.Deserialize<CreateMessageDto>(messageJson);
                if (incomingMessage is null)
                {
                    await SendToClient(webSocket, WebSocketMessageTypes.Error, new ErrorDto
                    {
                        Code = "INVALID_MESSAGE",
                        Message = "Invalid message format"
                    });
                    continue;
                }

                validationService.ValidateAndThrow(incomingMessage);

                var savedMessage = await messageService.CreateMessageAsync(incomingMessage, authenticatedUser);
                await BroadcastMessage(savedMessage);
            }
        }
        catch (ApiException ex)
        {
            logger.LogWarning("API error in WebSocket for user {Username}: {Message}", authenticatedUser.Username,
                ex.Message);
            await SendToClient(webSocket, WebSocketMessageTypes.Error, new ErrorDto
            {
                Code = ex.Code,
                Message = ex.Message,
                Errors = ex.Errors
            });
        }
        catch (WebSocketException ex)
        {
            logger.LogInformation("WebSocket connection closed for user {Username}: {Message}",
                authenticatedUser.Username, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling WebSocket connection for user {Username}", authenticatedUser.Username);
            await SendToClient(webSocket, WebSocketMessageTypes.Error, new ErrorDto
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred"
            });
        }
        finally
        {
            Clients.TryRemove(webSocket, out _);
            logger.LogInformation("Client disconnected: {Username}. Total clients: {Count}",
                authenticatedUser.Username, Clients.Count);
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

        foreach (var client in Clients.Keys)
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
            Clients.TryRemove(socket, out _);
        }
    }
}
