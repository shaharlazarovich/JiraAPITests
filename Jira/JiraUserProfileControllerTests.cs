using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraUserProfileControllerTests
{
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraIssueService> _mockIssueService;
    private readonly Mock<JiraUserProfileService> _mockProfileService;
    private readonly Mock<ILogger<JiraUserProfileController>> _mockLogger;
    private readonly JiraUserProfileController _controller;

    public JiraUserProfileControllerTests()
    {
        // Set up mocks for the required dependencies
        _mockUserService = new Mock<JiraUserService>();
        _mockIssueService = new Mock<JiraIssueService>();
        _mockProfileService = new Mock<JiraUserProfileService>();
        _mockLogger = new Mock<ILogger<JiraUserProfileController>>();

        // Instantiate the controller with mocks
        _controller = new JiraUserProfileController(
            _mockLogger.Object,
            _mockUserService.Object,
            _mockIssueService.Object,
            _mockProfileService.Object
        );
    }

    [Fact]
    public async Task GetUserProfile_ReturnsOkWithData()
    {
        // Arrange
        var mockProfile = new JiraUserProfile { Id = "1", UserId = "1" };
        _mockProfileService.Setup(s => s.GetUserProfile("1")).ReturnsAsync(mockProfile);

        // Act
        var result = await _controller.GetUserProfile("1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<JiraUserProfile>(okResult.Value);
        Assert.Equal("1", data.Id);
        Assert.Equal("1", data.UserId);
    }

    [Fact]
    public async Task GetUserProfile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        // Arrange
        _mockProfileService.Setup(s => s.GetUserProfile("1")).ReturnsAsync((JiraUserProfile?)null);

        // Act
        var result = await _controller.GetUserProfile("1");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUserProfile_ReturnsCreatedResult()
    {
        // Arrange
        var newProfile = new JiraUserProfile { UserId = "1" };
        var createdProfile = new JiraUserProfile { Id = "1", UserId = "1" };

        _mockProfileService.Setup(s => s.AddUserProfile(It.IsAny<JiraUserProfile>())).ReturnsAsync(createdProfile);

        // Act
        var result = await _controller.AddUserProfile(newProfile);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<JiraUserProfile>(createdResult.Value);
        Assert.Equal("1", data.Id);
        Assert.Equal("1", data.UserId);
    }

    [Fact]
    public async Task AddUserProfile_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("UserId", "Required");

        // Act
        var result = await _controller.AddUserProfile(new JiraUserProfile());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
