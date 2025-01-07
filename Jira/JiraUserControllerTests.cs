using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraUserControllerTests
{
    private readonly Mock<JiraUserService> _mockService;
    private readonly Mock<ILogger<JiraUserController>> _mockLogger;
    private readonly JiraUserController _controller;

    public JiraUserControllerTests()
    {
        _mockService = new Mock<JiraUserService>();
        _mockLogger = new Mock<ILogger<JiraUserController>>();
        _controller = new JiraUserController(_mockService.Object);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithData()
    {
        var mockData = new List<User>
        {
            new JiraUser { Id = "1", Name = "User1" },
            new JiraUser { Id = "2", Name = "User2" }
        };
        _mockService.Setup(s => s.GetAllUsers()).ReturnsAsync(mockData);

        var result = await _controller.GetAllUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<User>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task AddUser_ReturnsCreatedResult()
    {
        var newUser = new JiraUser { Name = "User1" };
        _mockService.Setup(s => s.AddUser(It.IsAny<JiraUser>())).ReturnsAsync(new JiraUser { Id = "1", Name = "User1" });

        var result = await _controller.AddUser(newUser);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<User>(createdResult.Value);
        Assert.Equal("1", data.Id);
    }

    [Fact]
    public async Task AddUser_ReturnsBadRequest_WhenModelInvalid()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.AddUser(new JiraUser());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsOkWithData()
    {
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

        _mockService.Setup(s => s.FetchAndSaveUsers(credentials.BaseUrl, credentials.Username, credentials.Token))
            .ReturnsAsync(mockData);

        var result = await _controller.FetchAndSaveUsers(credentials);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<User>>(okResult.Value);

        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsBadRequest_OnInvalidRequest()
    {
        var request = new FetchUsersRequest(); // Provide an empty request object instead of null.

        var result = await _controller.FetchAndSaveUsers(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        Assert.Contains("required fields", badRequestResult.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsInternalServerError_OnException()
    {
        var credentials = new JiraCredentials
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "test@example.com",
            Token = "api-token"
        };

        _mockService.Setup(s => s.FetchAndSaveUsers(credentials.BaseUrl, credentials.Username, credentials.Token))
            .ThrowsAsync(new Exception("Service error"));

        var result = await _controller.FetchAndSaveUsers(credentials);

        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }
}
