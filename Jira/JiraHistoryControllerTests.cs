using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraHistoryControllerTests
{
    private readonly Mock<JiraHistoryService> _mockHistoryService;
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraIssueService> _mockIssueService;
    private readonly Mock<ILogger<JiraHistoryController>> _mockLogger;
    private readonly JiraHistoryController _controller;

    public JiraHistoryControllerTests()
    {
        // Initialize service mocks
        _mockHistoryService = new Mock<JiraHistoryService>();
        _mockUserService = new Mock<JiraUserService>();
        _mockIssueService = new Mock<JiraIssueService>();
        _mockLogger = new Mock<ILogger<JiraHistoryController>>();

        // Instantiate the controller with the necessary mocks
        _controller = new JiraHistoryController(
            _mockLogger.Object,
            _mockUserService.Object,
            _mockIssueService.Object,
            _mockHistoryService.Object
        );
    }

   [Fact]
    public async Task FetchAndSaveIssueHistory_ReturnsOkResult()
    {
        // Arrange
        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            Token = "testToken"
        };

        var mockHistories = new List<JiraIssueHistory>
        {
            new JiraIssueHistory { FieldChanged = "Status", OldValue = "Open", NewValue = "In Progress" }
        };

        _mockHistoryService
            .Setup(s => s.FetchAndSaveIssueHistory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mockHistories);

        // Act
        var result = await _controller.FetchAndSaveIssueHistory(credentials);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraIssueHistory>>(okResult.Value);

        // Verify the collection size
        Assert.Single(data);

        // Verify the content of the collection
        var history = data.First();
        Assert.Equal("Status", history.FieldChanged);
    }

    [Fact]
    public async Task FetchAndSaveIssueHistory_Returns500_OnException()
    {
        // Arrange
        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            Token = "testToken"
        };

        _mockHistoryService
            .Setup(s => s.FetchAndSaveIssueHistory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.FetchAndSaveIssueHistory(credentials);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("Test exception", objectResult?.Value?.ToString());
    }
}
