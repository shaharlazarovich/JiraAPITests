using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ActivityTypeControllerTests
{
    private readonly Mock<IActivityTypeService> _mockService;
    private readonly Mock<ILogger<ActivityTypeController>> _mockLogger;
    private readonly ActivityTypeController _controller;

    public ActivityTypeControllerTests()
    {
        _mockService = new Mock<IActivityTypeService>();
        _mockLogger = new Mock<ILogger<ActivityTypeController>>();
        _controller = new ActivityTypeController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllActivityTypes_ReturnsOkWithData()
    {
        // Arrange
        var mockData = new List<ActivityType>
        {
            new ActivityType { Id = 1, Name = "Type1" },
            new ActivityType { Id = 2, Name = "Type2" }
        };
        _mockService.Setup(s => s.GetAllActivityTypes()).ReturnsAsync(mockData);

        // Act
        var result = await _controller.GetAllActivityTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<ActivityType>>(okResult.Value);
        Assert.Equal(2, data.Count);
        Assert.Equal("Type1", data[0].Name);
    }

    [Fact]
    public async Task AddActivityType_ReturnsCreatedResult()
    {
        // Arrange
        var newActivityType = new ActivityType { Name = "Type1" };
        var createdActivityType = new ActivityType { Id = 1, Name = "Type1" };
        _mockService.Setup(s => s.AddActivityType(It.IsAny<ActivityType>())).ReturnsAsync(createdActivityType);

        // Act
        var result = await _controller.AddActivityType(newActivityType);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var data = Assert.IsAssignableFrom<ActivityType>(createdResult.Value);
        Assert.Equal(1, data.Id);
        Assert.Equal("Type1", data.Name);
    }

    [Fact]
    public async Task AddActivityType_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _controller.AddActivityType(new ActivityType());

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
