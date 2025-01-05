using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JiraApiProject;
using JiraApiProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

public class JiraControllerTests
{
    private readonly JiraController _jiraController;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<JiraSettings>> _mockSettings;
    private readonly JiraSettings _settings;
    private readonly Mock<ILogger<JiraService>> _mockLogger;
    private readonly Mock<ILogger<JiraController>> _mockLoggerController;
    private readonly JiraDbContext _dbContext;
    private readonly Mock<UserService> _mockUserService;
    private readonly Mock<UserActivityService> _mockUserActivityService;
    private readonly Mock<UserProfileService> _mockUserProfileService;
    private readonly Mock<JiraHistoryService> _mockJiraHistoryService;
    private readonly JiraService _jiraService;    public JiraControllerTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockLogger = new Mock<ILogger<JiraService>>();
        _mockLoggerController = new Mock<ILogger<JiraController>>();

        _settings = new JiraSettings
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            ApiToken = "apitoken"
        };

        _mockSettings = new Mock<IOptions<JiraSettings>>();
        _mockSettings.Setup(s => s.Value).Returns(_settings);

         // Mocking the JiraDbContext
        var dbContextOptions = new DbContextOptionsBuilder<JiraDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _dbContext = new JiraDbContext(dbContextOptions)
        {
            Users = new List<User>().AsQueryable().BuildMockDbSet().Object,
            UserActivities = new List<UserActivity>().AsQueryable().BuildMockDbSet().Object,
            UserProfiles = new List<UserProfile>().AsQueryable().BuildMockDbSet().Object,
            ActivityTypes = new List<ActivityType>().AsQueryable().BuildMockDbSet().Object,
            JiraIssues = new List<Issue>().AsQueryable().BuildMockDbSet().Object,
            IssueHistories = new List<IssueHistory>().AsQueryable().BuildMockDbSet().Object
        };
         _mockUserService = new Mock<UserService>(_dbContext);
        _mockUserActivityService = new Mock<UserActivityService>(_dbContext);
        _mockUserProfileService = new Mock<UserProfileService>(_dbContext);
        _mockJiraHistoryService = new Mock<JiraHistoryService>(_dbContext);

        _jiraService = new JiraService(
            _httpClient,
            _mockSettings.Object,
            _mockLogger.Object,
            _dbContext,
            _mockUserService.Object,
            _mockUserActivityService.Object,
            _mockUserProfileService.Object,
            _mockJiraHistoryService.Object
        );
        _jiraController = new JiraController(_jiraService, _mockLoggerController.Object);
    }

    private void SetupHttpResponseForIssues(IEnumerable<object>? issues)
    {
        var responseContent = new { issues };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == $"{_settings.BaseUrl}/rest/api/3/search"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseContent))
            });
    }

    private void SetupHttpErrorResponse(HttpStatusCode statusCode)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });
    }

    private void SetupMalformedHttpResponse()
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("INVALID_JSON")
            });
    }

    [Fact]
    public async Task GetIssues_ReturnsOkResultWithData()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var mockedResponse = @"
        {
            ""issues"": [
                { ""id"": ""1"", ""key"": ""TEST-1"", ""fields"": { ""summary"": ""Test Issue 1"", ""description"": null, ""status"": { ""name"": ""To Do"" } } },
                { ""id"": ""2"", ""key"": ""TEST-2"", ""fields"": { ""summary"": ""Test Issue 2"", ""description"": null, ""status"": { ""name"": ""Done"" } } }
            ]
        }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockedResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://mock-jira-instance.com")
        };

        var settings = new JiraSettings
        {
            BaseUrl = "https://mock-jira-instance.com",
            Username = "mock-user",
            ApiToken = "mock-token"
        };
        var options = Options.Create(settings);

        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "testuser",
            JiraToken = "apitoken"
        };

        var result = await _jiraController.GetIssues(credentials);

        var okResult = Assert.IsType<ObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var data = Assert.IsAssignableFrom<List<Issue>>(okResult.Value);

        Assert.Equal(2, data.Count);
        Assert.Contains(data, issue => issue.Key == "TEST-1");
        Assert.Contains(data, issue => issue.Key == "TEST-2");
    }

    [Fact]
    public async Task GetIssues_ReturnsNotFound_WhenServiceReturnsEmptyList()
    {
        SetupHttpResponseForIssues(Enumerable.Empty<object>());

        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "testuser",
            JiraToken = "apitoken"
        };

        var result = await _jiraController.GetIssues(credentials);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetIssues_ReturnsNotFound_WhenServiceReturnsNull()
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"issues\": null}")
            });
        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "testuser",
            JiraToken = "apitoken"
        };

        var result = await _jiraController.GetIssues(credentials);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetIssues_ReturnsInternalServerError_WhenServiceThrowsException()
    {
        SetupHttpErrorResponse(HttpStatusCode.InternalServerError);

        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "testuser",
            JiraToken = "apitoken"
        };

        var result = await _jiraController.GetIssues(credentials);
        var statusCodeResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetIssues_HandlesMalformedResponse()
    {
        SetupMalformedHttpResponse();

        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "testuser",
            JiraToken = "apitoken"
        };

        var result = await _jiraController.GetIssues(credentials);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Malformed JSON response.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetIssuesFromDatabase_ReturnsOkResultWithData()
    {
        // Arrange
        var mockedResponse = new List<Issue>
        {
            new Issue { Key = "TEST-1", Fields = new Fields { Summary = "Summary 1", Description = "Description 1" } },
            new Issue { Key = "TEST-2", Fields = new Fields { Summary = "Summary 2", Description = "Description 2" } }
        };

        var mockJiraService = new Mock<IJiraService>();
        mockJiraService.Setup(s => s.GetIssuesFromDatabase()).ReturnsAsync(mockedResponse);

        var controller = new JiraController(mockJiraService.Object, Mock.Of<ILogger<JiraController>>());

        // Act
        var result = await controller.GetIssuesFromDatabase();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<Issue>>(okResult.Value);

        Assert.Equal(2, data.Count);
        Assert.Contains(data, i => i.Key == "TEST-1");
        Assert.Contains(data, i => i.Key == "TEST-2");
    }

    [Fact]
    public async Task GetIssuesFromDatabase_ReturnsNotFound_WhenNoData()
    {
        var mockJiraService = new Mock<IJiraService>();
        mockJiraService.Setup(s => s.GetIssuesFromDatabase()).ReturnsAsync(new List<Issue>());

        var controller = new JiraController(mockJiraService.Object, Mock.Of<ILogger<JiraController>>());

        var result = await controller.GetIssuesFromDatabase();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveIssues_CallsServiceToSaveData()
    {
        var mockJiraService        = new Mock<IJiraService>();

        mockJiraService
            .Setup(s => s.GetAndSaveIssues(It.IsAny<JiraCredentials>()))
            .ReturnsAsync(new List<Issue>
            {
                new Issue { Key = "TEST-1", Fields = new Fields { Summary = "Summary 1" } },
                new Issue { Key = "TEST-2", Fields = new Fields { Summary = "Summary 2" } }
            });

        var controller = new JiraController(mockJiraService.Object, Mock.Of<ILogger<JiraController>>());

        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "user",
            JiraToken = "token"
        };

        var result = await _jiraController.FetchAndSaveIssues(credentials);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<Issue>>(okResult.Value);

        Assert.Equal(2, data.Count);
        Assert.Contains(data, issue => issue.Key == "TEST-1");
        Assert.Contains(data, issue => issue.Key == "TEST-2");
        mockJiraService.Verify(s => s.GetAndSaveIssues(It.IsAny<JiraCredentials>()), Times.Once);
    }

    [Fact]
    public async Task FetchAndSaveIssues_ReturnsBadRequest_OnInvalidCredentials()
    {
        var credentials = new JiraCredentials
        {
            JiraUrl = null,
            JiraUser = null,
            JiraToken = null
        };

        var result = await _jiraController.FetchAndSaveIssues(credentials);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FetchAndSaveIssues_ReturnsInternalServerError_OnServiceException()
    {
        var mockJiraService = new Mock<IJiraService>();

        mockJiraService
            .Setup(s => s.GetAndSaveIssues(It.IsAny<JiraCredentials>()))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = new JiraController(mockJiraService.Object, Mock.Of<ILogger<JiraController>>());

        var credentials = new JiraCredentials
        {
            JiraUrl = "https://example.atlassian.net",
            JiraUser = "user",
            JiraToken = "token"
        };

        var result = await controller.FetchAndSaveIssues(credentials);

        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
        Assert.Equal("An error occurred while fetching and saving issues.", errorResult.Value);
    }
}

