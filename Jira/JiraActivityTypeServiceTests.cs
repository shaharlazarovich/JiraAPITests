using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiraApiProject.Services;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Xunit;

public class JiraActivityTypeServiceTests
{
    private readonly JiraDbContext _dbContext;
    private readonly JiraActivityTypeService _activityTypeService;

    public JiraActivityTypeServiceTests()
    {
        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        // Initialize all required DbSet properties
        _dbContext = new JiraDbContext(options)
        {
            Users = new List<JiraUser>().AsQueryable().BuildMockDbSet().Object,
            UserActivities = new List<JiraUserActivity>().AsQueryable().BuildMockDbSet().Object,
            UserProfiles = new List<JiraUserProfile>().AsQueryable().BuildMockDbSet().Object,
            ActivityTypes = new List<JiraActivityType>().AsQueryable().BuildMockDbSet().Object,
            Issues = new List<JiraIssue>().AsQueryable().BuildMockDbSet().Object,
            IssueHistories = new List<JiraIssueHistory>().AsQueryable().BuildMockDbSet().Object,
        };

        _activityTypeService = new JiraActivityTypeService(_dbContext);
    }

    [Fact]
    public async Task GetAllActivityTypes_ReturnsCorrectData()
    {
        // Arrange
        var activityTypes = new List<JiraActivityType>
        {
            new JiraActivityType { Id = "1", Name = "Created Issue" },
            new JiraActivityType { Id = "2", Name = "Updated Issue" },
        };

        await _dbContext.ActivityTypes.AddRangeAsync(activityTypes);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _activityTypeService.GetAllActivityTypesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, at => at.Name == "Created Issue");
        Assert.Contains(result, at => at.Name == "Updated Issue");
    }

    [Fact]
    public async Task AddActivityType_ShouldAddNewType()
    {
        // Arrange
        var newActivityType = new JiraActivityType { Id = "3", Name = "Deleted Issue" };

        // Act
        await _activityTypeService.AddActivityTypeAsync(newActivityType);

        // Assert
        var result = _dbContext.ActivityTypes.SingleOrDefault(at => at.Id == "3");
        Assert.NotNull(result);
        Assert.Equal("Deleted Issue", result?.Name);
    }
}
