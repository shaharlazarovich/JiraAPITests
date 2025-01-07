using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Controllers;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class JiraActivityTypeControllerTests
{
    private readonly Mock<JiraActivityTypeService> _mockActivityTypeService;
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraIssueService> _mockIssueService;
    private readonly Mock<ILogger<JiraActivityTypeController>> _mockLogger;
    private readonly JiraActivityTypeController _controller;

    public JiraActivityTypeControllerTests()
    {
        // Initialize service mocks
        _mockActivityTypeService = new Mock<JiraActivityTypeService>();
        _mockUserService = new Mock<JiraUserService>();
        _mockIssueService = new Mock<JiraIssueService>();
        _mockLogger = new Mock<ILogger<JiraActivityTypeController>>();

        // Instantiate the controller with the necessary mocks
        _controller = new JiraActivityTypeController(
            _mockLogger.Object,
            _mockUserService.Object,
            _mockIssueService.Object,
            _mockActivityTypeService.Object
        );
    }

    [Fact]
    public async Task GetAllActivityTypes_ReturnsOkWithData()
    {
        // Arrange
        var mockData = new List<JiraActivityType>
        {
            new JiraActivityType { Id = "1", Name = "Type1" },
            new JiraActivityType { Id = "2", Name = "Type2" }
        };
        _mockActivityTypeService.Setup(s => s.GetAllActivityTypesAsync()).ReturnsAsync(mockData);

        // Act
        var result = await _controller.GetAllActivityTypes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<JiraActivityType>>(okResult.Value);
        Assert.Equal(2, data.Count);
        Assert.Equal("Type1", data[0].Name);
    }

    [Fact]
public async Task AddActivityType_ReturnsCreatedResult()
{
    // Arrange
    var newActivityType = new JiraActivityType { Id = "1", Name = "Type1" };
    var createdActivityType = new JiraActivityType { Id = "1", Name = "Type1" };

    // Mocking the AddActivityTypeAsync method
    _mockActivityTypeService
        .Setup(s => s.AddActivityTypeAsync(It.IsAny<JiraActivityType>()))
        .Callback<JiraActivityType>(input => input.Id = "1") // Simulate setting the ID
        .Returns(Task.CompletedTask); // Return a completed task since the method doesn't return a value

    // Act
    var result = await _controller.AddActivityType(newActivityType);

    // Assert
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var data = Assert.IsAssignableFrom<JiraActivityType>(createdResult.Value);
    Assert.Equal("1", data.Id);
    Assert.Equal("Type1", data.Name);
}


    [Fact]
    public async Task AddActivityType_ReturnsBadRequest_WhenModelInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _controller.AddActivityType(new JiraActivityType{ Id = "1", Name = "Type1" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
