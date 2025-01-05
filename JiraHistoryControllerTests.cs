using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

public class JiraHistoryControllerTests
{
    private Mock<IJiraHistoryService> _mockService;
    private readonly Mock<ILogger<JiraHistoryController>> _mockLogger;
    private readonly JiraHistoryController _controller;

    public JiraHistoryControllerTests()
    {
        _mockService = new Mock<IJiraHistoryService>();
        _mockLogger = new Mock<ILogger<JiraHistoryController>>();
        _controller = new JiraHistoryController(_mockService.Object);
    }

    [Fact]
    public async Task FetchAndSaveIssueHistory_ReturnsOkResult()
    {
        // Arrange
        var setting = new JiraSettings
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            ApiToken = "testToken"
        };

        _mockService
            .Setup(s => s.FetchAndSaveIssueHistory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<IssueHistory> { new IssueHistory { FieldChanged = "Status", OldValue = "Open", NewValue = "In Progress" } });

        // Act
        var result = await _controller.FetchAndSaveIssueHistory(setting);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task FetchAndSaveIssueHistory_Returns500_OnException()
    {
        // Arrange
        var setting = new JiraSettings
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            ApiToken = "testToken"
        };

        _mockService
            .Setup(s => s.FetchAndSaveIssueHistory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.FetchAndSaveIssueHistory(setting);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}
