using backend.Controllers;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<MessagesController>> _mockLogger;
    private readonly MessagesController _controller;

    public MessagesControllerTests()
    {
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<MessagesController>>();
        _controller = new MessagesController(_mockMessageService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetMessages_ReturnsOkResult_WithListOfMessages()
    {
        // Arrange
        var expectedMessages = new List<Message>
        {
            new() { Id = 1, Creator = "TestUser1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Creator = "TestUser2", CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Creator = "TestUser3", CreatedAt = DateTime.UtcNow }
        };

        _mockMessageService.Setup(s => s.GetRecentMessagesAsync(It.IsAny<int>()))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetMessages(50);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMessages = Assert.IsAssignableFrom<IEnumerable<Message>>(okResult.Value);
        Assert.Equal(3, returnedMessages.Count());
    }
}
