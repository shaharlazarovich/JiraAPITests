using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MockQueryable.Moq;
using JiraApiProject.Services;

public class JiraHistoryServiceTests
{
    private readonly JiraDbContext _dbContext;
    private readonly JiraHistoryService _jiraHistoryService;

    public JiraHistoryServiceTests()
    {
        // Set up in-memory database options
        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase("JiraHistoryTestDb")
            .Options;

        // Initialize DbContext with mock DbSets
        _dbContext = new JiraDbContext(options)
        {
            Users = new List<JiraUser>().AsQueryable().BuildMockDbSet().Object,
            UserActivities = new List<JiraUserActivity>().AsQueryable().BuildMockDbSet().Object,
            UserProfiles = new List<JiraUserProfile>().AsQueryable().BuildMockDbSet().Object,
            ActivityTypes = new List<JiraActivityType>().AsQueryable().BuildMockDbSet().Object,
            Issues = new List<JiraIssue>().AsQueryable().BuildMockDbSet().Object,
            IssueHistories = new List<JiraIssueHistory>().AsQueryable().BuildMockDbSet().Object,
        };

        // Initialize JiraHistoryService with the DbContext
        _jiraHistoryService = new JiraHistoryService(_dbContext);
    }

    [Fact]
    public async Task SaveIssueHistory_ShouldSaveHistoryRecords()
    {
        // Arrange
        var issue = new JiraIssue { Key = "TEST-1" };
        await _dbContext.Issues.AddAsync(issue);
        await _dbContext.SaveChangesAsync();

        var issueHistories = new List<JiraIssueHistory>
        {
            new JiraIssueHistory
            {
                IssueId = issue.Id,
                FieldChanged = "Status",
                OldValue = "Open",
                NewValue = "Closed"
            }
        };

        // Act
        await _dbContext.IssueHistories.AddRangeAsync(issueHistories);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedHistories = _dbContext.IssueHistories.ToList();
        Assert.Single(savedHistories);
        Assert.Equal("Status", savedHistories[0].FieldChanged);
    }

    [Fact]
    public async Task FetchAndSaveIssueHistory_ShouldSaveNewHistories()
    {
        // Arrange
        var issue = new JiraIssue { Key = "TEST-1" };
        await _dbContext.Issues.AddAsync(issue);
        await _dbContext.SaveChangesAsync();

        var issueHistories = new List<JiraIssueHistory>
        {
            new JiraIssueHistory
            {
                IssueId = issue.Id,
                FieldChanged = "Status",
                OldValue = "To Do",
                NewValue = "In Progress",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "user1"
            }
        };

        // Act
        await _dbContext.IssueHistories.AddRangeAsync(issueHistories);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedHistories = await _dbContext.IssueHistories.ToListAsync();
        Assert.Single(savedHistories);
        Assert.Equal("Status", savedHistories[0].FieldChanged);
        Assert.Equal("To Do", savedHistories[0].OldValue);
        Assert.Equal("In Progress", savedHistories[0].NewValue);
        Assert.Equal("user1", savedHistories[0].ChangedBy);
    }
}
