using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using JiraApiProject.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JiraApiProject;
using MockQueryable.Moq;

public class JiraIssueServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<JiraSettings> _settings;
    private readonly JiraIssueService _jiraService;
    private readonly JiraDbContext _dbContext;
    private readonly Mock<ILogger<JiraIssueService>> _mockLogger;

    // Additional mocks for new dependencies
    private readonly Mock<JiraUserService> _mockUserService;
    private readonly Mock<JiraUserActivityService> _mockUserActivityService;
    private readonly Mock<JiraUserProfileService> _mockUserProfileService;
    private readonly Mock<JiraHistoryService> _mockJiraHistoryService;

    public JiraIssueServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/")
        };

        var options = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _dbContext = new JiraDbContext(options)
        {
            Users = new List<JiraUser>().AsQueryable().BuildMockDbSet().Object,
            UserActivities = new List<JiraUserActivity>().AsQueryable().BuildMockDbSet().Object,
            UserProfiles = new List<JiraUserProfile>().AsQueryable().BuildMockDbSet().Object,
            ActivityTypes = new List<JiraActivityType>().AsQueryable().BuildMockDbSet().Object,
            Issues = new List<JiraIssue>().AsQueryable().BuildMockDbSet().Object,
            IssueHistories = new List<JiraIssueHistory>().AsQueryable().BuildMockDbSet().Object
        };

        _mockLogger = new Mock<ILogger<JiraIssueService>>();
        _settings = Options.Create(new JiraSettings
        {
            BaseUrl = "https://test.atlassian.net",
            Username = "user",
            ApiToken = "api-token"
        });

        // Initialize mocks for additional dependencies
        _mockUserService = new Mock<JiraUserService>(
            new Mock<ILogger<JiraUserService>>().Object,
            _dbContext,
            _httpClient);

        _mockUserActivityService = new Mock<JiraUserActivityService>(
            new Mock<ILogger<JiraUserActivityService>>().Object,
            _dbContext,
            _httpClient);

        _mockUserProfileService = new Mock<JiraUserProfileService>(
            _dbContext,
            new Mock<ILogger<JiraUserProfileService>>().Object);

        _mockJiraHistoryService = new Mock<JiraHistoryService>(
            new Mock<ILogger<JiraHistoryService>>().Object,
            _dbContext,
            _httpClient);

        _jiraService = new JiraIssueService(
            _httpClient,
            _settings,
            _mockLogger.Object,
            _dbContext,
            _mockUserService.Object,
            _mockUserActivityService.Object,
            _mockUserProfileService.Object,
            _mockJiraHistoryService.Object);
    }

    [Fact]
    public async Task GetIssues_ReturnsIssues()
    {
        var mockIssues = new[]
        {
            new { id = "1", key = "TEST-1", fields = new { summary = "Summary 1", description = "Description 1" } },
            new { id = "2", key = "TEST-2", fields = new { summary = "Summary 2", description = "Description 2" } }
        };

        SetupHttpResponse(HttpStatusCode.OK, new { issues = mockIssues });

        var result = await _jiraService.GetIssues();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, issue => issue.Key == "TEST-1");
        Assert.Contains(result, issue => issue.Key == "TEST-2");
    }

    [Fact]
    public async Task GetIssues_ReturnsEmptyList_WhenNoIssues()
    {
        SetupHttpResponse(HttpStatusCode.OK, new { issues = new object[0] });

        var result = await _jiraService.GetIssues();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveIssuesToDatabase_ShouldSaveIssues()
    {
        var newIssues = new List<JiraIssue>
        {
            new JiraIssue
            {
                Key = "TEST-1",
                Fields = new Fields
                {
                    Summary = "Test Summary",
                    Description = "Test Description",
                    Status = new Status { Name = "To Do" },
                    Updated = DateTime.UtcNow
                }
            }
        };

        await _jiraService.SaveIssuesToDatabase(newIssues);

        var savedIssues = await _dbContext.Issues
            .Include(i => i.Fields)
            .ThenInclude(f => f!.Status)
            .ToListAsync();

        Assert.Single(savedIssues);
        Assert.Equal("TEST-1", savedIssues[0].Key);
    }

    [Fact]
    public async Task SaveIssuesToDatabase_ShouldNotDuplicateKeys()
    {
        var issue = new JiraIssue
        {
            Key = "TEST-1",
            Fields = new Fields
            {
                Summary = "Initial Summary",
                Updated = DateTime.UtcNow
            }
        };
        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync();

        var updatedIssue = new JiraIssue
        {
            Key = "TEST-1",
            Fields = new Fields
            {
                Summary = "Updated Summary",
                Updated = DateTime.UtcNow.AddMinutes(10)
            }
        };

        await _jiraService.SaveIssuesToDatabase(new List<JiraIssue> { updatedIssue });

        var savedIssues = await _dbContext.Issues.ToListAsync();

        Assert.Single(savedIssues);
        Assert.Equal("Updated Summary", savedIssues[0]?.Fields?.Summary);
    }

    [Fact]
    public async Task GetIssuesFromDatabase_ShouldReturnSavedIssues()
    {
        var issue = new JiraIssue
        {
            Key = "TEST-1",
            Fields = new Fields
            {
                Summary = "Test Summary",
                Updated = DateTime.UtcNow
            }
        };

        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync();

        var issues = await _jiraService.GetIssuesFromDatabase();

        Assert.Single(issues);
        Assert.Equal("TEST-1", issues[0].Key);
    }


    private void SetupHttpResponse(HttpStatusCode statusCode, object? responseContent)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonConvert.SerializeObject(responseContent))
            });
    }
}
