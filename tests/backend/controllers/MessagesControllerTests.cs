using backend.Controllers;
using backend.DTOs;
using backend.Services;

using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Backend.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly MessagesController _controller;

    public MessagesControllerTests()
    {
        _mockMessageService = new Mock<IMessageService>();
        var mockWebSocketService = new Mock<IWebSocketService>();
        var mockValidationService = new Mock<IValidationService>();
        _controller = new MessagesController(_mockMessageService.Object, mockWebSocketService.Object, mockValidationService.Object);
    }

    [Fact]
    public async Task GetMessages_ReturnsOkResult_WithListOfMessages()
    {
        // Arrange
        var user = new MessageUserDto(1, "TestUser", "#ffffff");
        var expectedMessages = new List<MessageResponseDto>
        {
            new(1, "Hello", DateTime.UtcNow, null, user, []),
            new(2, "World", DateTime.UtcNow, null, user, []),
            new(3, "!", DateTime.UtcNow, null, user, [])
        };

        _mockMessageService.Setup(s => s.GetRecentMessagesAsync(It.IsAny<int>()))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetMessages(50);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMessages = Assert.IsAssignableFrom<IEnumerable<MessageResponseDto>>(okResult.Value);
        Assert.Equal(3, returnedMessages.Count());
    }
}
