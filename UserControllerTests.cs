using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class UserControllerTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _mockService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        _controller = new UserController(_mockService.Object);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithData()
    {
        var mockData = new List<User>
        {
            new User { Id = 1, Name = "User1" },
            new User { Id = 2, Name = "User2" }
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
        var newUser = new User { Name = "User1" };
        _mockService.Setup(s => s.AddUser(It.IsAny<User>())).ReturnsAsync(new User { Id = 1, Name = "User1" });

        var result = await _controller.AddUser(newUser);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<User>(createdResult.Value);
        Assert.Equal(1, data.Id);
    }

    [Fact]
    public async Task AddUser_ReturnsBadRequest_WhenModelInvalid()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.AddUser(new User());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ReturnsOkWithData()
    {
        var mockData = new List<User>
        {
            new User { Id = 1, Name = "User1" },
            new User { Id = 2, Name = "User2" }
        };

        var request = new FetchUsersRequest
        {
            JiraBaseUrl = "https://example.atlassian.net",
            Email = "test@example.com",
            ApiToken = "api-token"
        };

        _mockService.Setup(s => s.FetchAndSaveUsers(request.JiraBaseUrl, request.Email, request.ApiToken))
            .ReturnsAsync(mockData);

        var result = await _controller.FetchAndSaveUsers(request);

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
        var request = new FetchUsersRequest
        {
            JiraBaseUrl = "https://example.atlassian.net",
            Email = "test@example.com",
            ApiToken = "api-token"
        };

        _mockService.Setup(s => s.FetchAndSaveUsers(request.JiraBaseUrl, request.Email, request.ApiToken))
            .ThrowsAsync(new Exception("Service error"));

        var result = await _controller.FetchAndSaveUsers(request);

        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
    }
}
