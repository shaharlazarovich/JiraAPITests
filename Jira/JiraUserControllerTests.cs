using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraUserControllerTests
{
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraIssueService> _mockIssueService;
    private readonly Mock<ILogger<JiraUserController>> _mockLogger;
    private readonly JiraUserController _controller;

    public JiraUserControllerTests()
    {
        // Initialize service mocks
        _mockUserService = new Mock<JiraUserService>();
        _mockIssueService = new Mock<JiraIssueService>();
        _mockLogger = new Mock<ILogger<JiraUserController>>();

        // Instantiate the controller with the necessary mocks
        _controller = new JiraUserController(
            _mockLogger.Object,
            _mockUserService.Object,
            _mockIssueService.Object
        );
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithData()
    {
        // Arrange
        var mockData = new List<JiraUser>
        {
            new JiraUser { Id = "1", Name = "User1" },
            new JiraUser { Id = "2", Name = "User2" }
        };
        _mockUserService.Setup(s => s.GetAllUsers()).ReturnsAsync(mockData);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraUser>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task AddUser_ReturnsCreatedResult()
    {
        // Arrange
        var newUser = new JiraUser { Name = "User1" };
        _mockUserService.Setup(s => s.AddUser(It.IsAny<JiraUser>()))
            .ReturnsAsync(new JiraUser { Id = "1", Name = "User1" });

        // Act
        var result = await _controller.AddUser(newUser);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<JiraUser>(createdResult.Value);
        Assert.Equal("1", data.Id);
    }

    [Fact]
    public async Task AddUser_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _controller.AddUser(new JiraUser());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsOkWithData()
    {
        // Arrange
        var mockData = new List<JiraUser>
        {
            new JiraUser { Id = "1", Name = "User1" },
            new JiraUser { Id = "2", Name = "User2" }
        };

        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            Token = "api-token"
        };

        _mockUserService.Setup(s => s.FetchAndSaveUsers(credentials.BaseUrl, credentials.Username, credentials.Token))
            .ReturnsAsync(mockData);

        // Act
        var result = await _controller.FetchAndSaveUsers(credentials);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraUser>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsBadRequest_OnInvalidRequest()
    {
        // Arrange
        var credentials = new JiraCredentials();

        // Act
        var result = await _controller.FetchAndSaveUsers(credentials);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsInternalServerError_OnException()
    {
        // Arrange
        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            Token = "api-token"
        };

        _mockUserService.Setup(s => s.FetchAndSaveUsers(credentials.BaseUrl, credentials.Username, credentials.Token))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.FetchAndSaveUsers(credentials);

        // Assert
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }
}
