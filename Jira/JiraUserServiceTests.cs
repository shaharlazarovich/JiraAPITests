using System.Collections.Generic;
using System.Threading.Tasks;
using JiraApiProject.Services;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Xunit;

public class JiraUserServiceTests
{
    private readonly JiraDbContext _dbContext;
    private readonly JiraUserService _userService;

    public JiraUserServiceTests()
    {
        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase("UserServiceTestDb")
            .Options;

        // Initialize DbSets explicitly to satisfy the required properties
        _dbContext = new JiraDbContext(options)
        {
            Users = new List<JiraUser>().AsQueryable().BuildMockDbSet().Object,
            UserActivities = new List<JiraUserActivity>().AsQueryable().BuildMockDbSet().Object,
            UserProfiles = new List<JiraUserProfile>().AsQueryable().BuildMockDbSet().Object,
            ActivityTypes = new List<JiraActivityType>().AsQueryable().BuildMockDbSet().Object,
            Issues = new List<JiraIssue>().AsQueryable().BuildMockDbSet().Object,
            IssueHistories = new List<JiraIssueHistory>().AsQueryable().BuildMockDbSet().Object,
        };

        _userService = new JiraUserService(_dbContext);
    }

    [Fact]
    public async Task FetchAndSaveUsers_ShouldSaveUsers()
    {
        // Arrange
        var users = new List<JiraUser>
        {
            new JiraUser { AccountId = "1", DisplayName = "User One", Email = "user1@example.com" },
            new JiraUser { AccountId = "2", DisplayName = "User Two", Email = "user2@example.com" }
        };

        // Act
        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedUsers = _dbContext.Users.ToList();
        Assert.Equal(2, savedUsers.Count);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        var users = await _userService.GetAllUsers();

        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task AddUser_ShouldAddUserSuccessfully()
    {
        var newUser = new JiraUser
        {
            AccountId = "12345",
            DisplayName = "Test User",
            Email = "test@example.com"
        };

        await _userService.AddUser(newUser);

        var usersInDb = await _dbContext.Users.ToListAsync();

        Assert.Single(usersInDb);
        Assert.Equal("12345", usersInDb[0].AccountId);
        Assert.Equal("Test User", usersInDb[0].DisplayName);
        Assert.Equal("test@example.com", usersInDb[0].Email);
    }
}
