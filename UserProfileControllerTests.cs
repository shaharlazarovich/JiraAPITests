using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class UserProfileControllerTests
{
    private readonly Mock<IUserProfileService> _mockService;
    private readonly Mock<ILogger<UserProfileController>> _mockLogger;
    private readonly UserProfileController _controller;

    public UserProfileControllerTests()
    {
        _mockService = new Mock<IUserProfileService>();
        _mockLogger = new Mock<ILogger<UserProfileController>>();
        _controller = new UserProfileController(_mockService.Object);
    }

    [Fact]
    public async Task GetUserProfile_ReturnsOkWithData()
    {
        // Arrange
        var mockProfile = new UserProfile { Id = 1, UserId = 1 };
        _mockService.Setup(s => s.GetUserProfile(1)).ReturnsAsync(mockProfile);

        // Act
        var result = await _controller.GetUserProfile(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<UserProfile>(okResult.Value);
        Assert.Equal(1, data.Id);
        Assert.Equal(1, data.UserId);
    }

    [Fact]
    public async Task GetUserProfile_ReturnsNotFound_WhenProfileDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetUserProfile(1)).ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _controller.GetUserProfile(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }


    [Fact]
    public async Task AddUserProfile_ReturnsCreatedResult()
    {
        // Arrange
        var newProfile = new UserProfile { UserId = 1 };
        var createdProfile = new UserProfile { Id = 1, UserId = 1 };

        _mockService.Setup(s => s.AddUserProfile(It.IsAny<UserProfile>())).ReturnsAsync(createdProfile);

        // Act
        var result = await _controller.AddUserProfile(newProfile);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<UserProfile>(createdResult.Value);
        Assert.Equal(1, data.Id);
        Assert.Equal(1, data.UserId);
    }

    [Fact]
    public async Task AddUserProfile_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("UserId", "Required");

        // Act
        var result = await _controller.AddUserProfile(new UserProfile());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
