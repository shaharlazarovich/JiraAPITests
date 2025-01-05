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
    public async Task AddActivityTypes_ShouldAddTypesToDatabase()
    {
        // Arrange
        var activityTypes = new List<ActivityType>
        {
            new ActivityType { Id = 1, Name = "Assigned Issue" },
            new ActivityType { Id = 2, Name = "Updated Description" }
        };

        // Act
        await _dbContext.ActivityTypes.AddRangeAsync(activityTypes);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedActivityTypes = _dbContext.ActivityTypes.ToList();
        Assert.Equal(2, savedActivityTypes.Count);
        Assert.Contains(savedActivityTypes, at => at.Name == "Assigned Issue");
    }

    [Fact]
    public async Task AddDuplicateActivityType_ShouldThrowException()
    {
        // Arrange
        var activityType = new ActivityType { Id = 1, Name = "Assigned Issue" };
        await _dbContext.ActivityTypes.AddAsync(activityType);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await _dbContext.ActivityTypes.AddAsync(activityType);
            await _dbContext.SaveChangesAsync();
        });
    }
}
