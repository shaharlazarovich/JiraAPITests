using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraIssueControllerTests
{
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraIssueService> _mockIssueService;
    private readonly Mock<ILogger<JiraIssueController>> _mockLogger;
    private readonly JiraIssueController _controller;

    public JiraIssueControllerTests()
    {
        // Initialize service mocks
        _mockUserService = new Mock<JiraUserService>();
        _mockIssueService = new Mock<JiraIssueService>();
        _mockLogger = new Mock<ILogger<JiraIssueController>>();

        // Instantiate the controller with the necessary mocks
        _controller = new JiraIssueController(
            _mockLogger.Object,
            _mockUserService.Object,
            _mockIssueService.Object
        );
    }

    [Fact]
    public async Task GetIssues_ReturnsOkResultWithData()
    {
        // Arrange
        var mockedIssues = new List<JiraIssue>
        {
            new JiraIssue { Key = "TEST-1", Fields = new Fields { Summary = "Test Issue 1" } },
            new JiraIssue { Key = "TEST-2", Fields = new Fields { Summary = "Test Issue 2" } }
        };

        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            Token = "apitoken"
        };

        _mockIssueService.Setup(s => s.GetIssuesWithPagination(credentials, It.IsAny<int>())).ReturnsAsync(mockedIssues);

        // Act
        var result = await _controller.GetIssues(credentials);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraIssue>>(okResult.Value);
        Assert.Equal(2, data.Count);
        Assert.Contains(data, issue => issue.Key == "TEST-1");
        Assert.Contains(data, issue => issue.Key == "TEST-2");
    }

    [Fact]
    public async Task GetIssues_ReturnsNotFound_WhenNoData()
    {
        // Arrange
        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            Token = "apitoken"
        };

        _mockIssueService.Setup(s => s.GetIssuesWithPagination(credentials, It.IsAny<int>())).ReturnsAsync(new List<JiraIssue>());

        // Act
        var result = await _controller.GetIssues(credentials);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveIssues_ReturnsOkResultWithData()
    {
        // Arrange
        var mockedIssues = new List<JiraIssue>
        {
            new JiraIssue { Key = "TEST-1", Fields = new Fields { Summary = "Test Issue 1" } },
            new JiraIssue { Key = "TEST-2", Fields = new Fields { Summary = "Test Issue 2" } }
        };

        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            Token = "apitoken"
        };

        _mockIssueService.Setup(s => s.GetAndSaveIssues(credentials)).ReturnsAsync(mockedIssues);

        // Act
        var result = await _controller.FetchAndSaveIssues(credentials);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraIssue>>(okResult.Value);
        Assert.Equal(2, data.Count);
        Assert.Contains(data, issue => issue.Key == "TEST-1");
        Assert.Contains(data, issue => issue.Key == "TEST-2");
    }

    [Fact]
    public async Task FetchAndSaveIssues_ReturnsBadRequest_OnInvalidCredentials()
    {
        // Arrange
        var credentials = new JiraCredentials();

        // Act
        var result = await _controller.FetchAndSaveIssues(credentials);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveIssues_ReturnsInternalServerError_OnServiceException()
    {
        // Arrange
        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            Token = "apitoken"
        };

        _mockIssueService.Setup(s => s.GetAndSaveIssues(credentials)).ThrowsAsync(new Exception("Service failure"));

        // Act
        var result = await _controller.FetchAndSaveIssues(credentials);

        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
        Assert.Equal("An error occurred while fetching and saving issues.", errorResult.Value);
    }
}
