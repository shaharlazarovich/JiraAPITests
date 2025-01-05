using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiraApiProject.Services;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Xunit;

public class ActivityTypeServiceTests
{
    private readonly JiraDbContext _dbContext;
    private readonly ActivityTypeService _activityTypeService;

    public ActivityTypeServiceTests()
    {
        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        // Initialize all required DbSet properties
        _dbContext = new JiraDbContext(options)
        {
            Users = new List<User>().AsQueryable().BuildMockDbSet().Object,
            UserActivities = new List<UserActivity>().AsQueryable().BuildMockDbSet().Object,
            UserProfiles = new List<UserProfile>().AsQueryable().BuildMockDbSet().Object,
            ActivityTypes = new List<ActivityType>().AsQueryable().BuildMockDbSet().Object,
            JiraIssues = new List<Issue>().AsQueryable().BuildMockDbSet().Object,
            IssueHistories = new List<IssueHistory>().AsQueryable().BuildMockDbSet().Object,
        };

        _activityTypeService = new ActivityTypeService(_dbContext);
    }

    [Fact]
    public async Task GetAllActivityTypes_ReturnsCorrectData()
    {
        // Arrange
        var activityTypes = new List<ActivityType>
        {
            new ActivityType { Id = 1, Name = "Created Issue" },
            new ActivityType { Id = 2, Name = "Updated Issue" },
        };

        await _dbContext.ActivityTypes.AddRangeAsync(activityTypes);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _activityTypeService.GetAllActivityTypes();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, at => at.Name == "Created Issue");
        Assert.Contains(result, at => at.Name == "Updated Issue");
    }

    [Fact]
    public async Task AddActivityType_ShouldAddNewType()
    {
        // Arrange
        var newActivityType = new ActivityType { Id = 3, Name = "Deleted Issue" };

        // Act
        await _activityTypeService.AddActivityType(newActivityType);

        // Assert
        var result = _dbContext.ActivityTypes.SingleOrDefault(at => at.Id == 3);
        Assert.NotNull(result);
        Assert.Equal("Deleted Issue", result?.Name);
    }
}
