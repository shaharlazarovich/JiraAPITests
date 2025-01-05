using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class UserActivityControllerTests
{
    private readonly Mock<IUserActivityService> _mockService;
    private readonly Mock<ILogger<UserActivityController>> _mockLogger;
    private readonly UserActivityController _controller;

    public UserActivityControllerTests()
    {
        _mockService = new Mock<IUserActivityService>();
        _mockLogger = new Mock<ILogger<UserActivityController>>();
        _controller = new UserActivityController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUserActivities_ReturnsOkWithData()
    {
        // Arrange
        var mockData = new List<UserActivity>
        {
            new UserActivity { Id = 1, ActivityTypeId = 1, UserId = 1 },
            new UserActivity { Id = 2, ActivityTypeId = 2, UserId = 1 }
        };

        _mockService.Setup(s => s.GetUserActivities(1)).ReturnsAsync(mockData);

        // Act
        var result = await _controller.GetUserActivities(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<UserActivity>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task GetUserActivities_ReturnsNotFound_WhenNoData()
    {
        // Arrange
        _mockService.Setup(s => s.GetUserActivities(1)).ReturnsAsync(new List<UserActivity>());

        // Act
        var result = await _controller.GetUserActivities(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUserActivity_ReturnsCreatedResult()
    {
        // Arrange
        var newUserActivity = new UserActivity { ActivityTypeId = 1, UserId = 1 };
        _mockService.Setup(s => s.AddUserActivity(It.IsAny<UserActivity>()))
            .ReturnsAsync(new UserActivity { Id = 1, ActivityTypeId = 1, UserId = 1 });

        // Act
        var result = await _controller.AddUserActivity(newUserActivity);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<UserActivity>(createdResult.Value);
        Assert.Equal(1, data.Id);
    }

    [Fact]
    public async Task AddUserActivity_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("ActivityTypeId", "Required");

        // Act
        var result = await _controller.AddUserActivity(new UserActivity());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
