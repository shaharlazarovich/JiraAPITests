using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraUserActivityControllerTests
{
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraIssueService> _mockIssueService;
    private readonly Mock<JiraUserActivityService> _mockActivityService;
    private readonly Mock<ILogger<JiraUserActivityController>> _mockLogger;
    private readonly JiraUserActivityController _controller;

    public JiraUserActivityControllerTests()
    {
        // Set up mocks for the required dependencies
        _mockUserService = new Mock<JiraUserService>();
        _mockIssueService = new Mock<JiraIssueService>();
        _mockActivityService = new Mock<JiraUserActivityService>();
        _mockLogger = new Mock<ILogger<JiraUserActivityController>>();

        // Instantiate the controller with mocks
        _controller = new JiraUserActivityController(
            _mockLogger.Object,
            _mockUserService.Object,
            _mockIssueService.Object,
            _mockActivityService.Object
        );
    }

    [Fact]
    public async Task GetUserActivities_ReturnsOkWithData()
    {
        // Arrange
        var mockData = new List<JiraUserActivity>
        {
            new JiraUserActivity { Id = "1", ActivityTypeId = "1", UserId = "1" },
            new JiraUserActivity { Id = "2", ActivityTypeId = "2", UserId = "1" }
        };

        _mockActivityService.Setup(s => s.GetUserActivities("1")).ReturnsAsync(mockData);

        // Act
        var result = await _controller.GetUserActivities("1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraUserActivity>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task GetUserActivities_ReturnsNotFound_WhenNoData()
    {
        // Arrange
        _mockActivityService.Setup(s => s.GetUserActivities("1")).ReturnsAsync(new List<JiraUserActivity>());

        // Act
        var result = await _controller.GetUserActivities("1");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddUserActivity_ReturnsCreatedResult()
    {
        // Arrange
        var newUserActivity = new JiraUserActivity { ActivityTypeId = "1", UserId = "1" };
        _mockActivityService.Setup(s => s.AddUserActivity(It.IsAny<JiraUserActivity>()))
            .ReturnsAsync(new JiraUserActivity { Id = "1", ActivityTypeId = "1", UserId = "1" });

        // Act
        var result = await _controller.AddUserActivity(newUserActivity);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<JiraUserActivity>(createdResult.Value);
        Assert.Equal("1", data.Id);
    }

    [Fact]
    public async Task AddUserActivity_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("ActivityTypeId", "Required");

        // Act
        var result = await _controller.AddUserActivity(new JiraUserActivity());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
