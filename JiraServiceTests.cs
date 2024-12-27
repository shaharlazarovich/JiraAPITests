using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using JiraApiProject.Services; // Adjust namespace based on your project
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public class JiraServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<JiraSettings> _settings;
    private readonly JiraService _jiraService;
    private Mock<ILogger<JiraService>> _mockLogger;
    private ILogger<JiraService> _logger;

    public JiraServiceTests()
    {
        // Initialize _mockHttpMessageHandler
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        _mockLogger = new Mock<ILogger<JiraService>>();
        
        _logger = _mockLogger.Object; // Extract the mocked logger object

        // Create HttpClient with the mock handler
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/")
        };

        // Initialize settings
        var settings = new JiraSettings
        {
            BaseUrl = "https://test.atlassian.net",
            Username = "user",
            ApiToken = "api-token"
        };

        // Wrap settings in IOptions
        _settings = Options.Create(settings);

        // Initialize JiraService
        _jiraService = new JiraService(_httpClient, _settings, _logger); // Pass the mocked logger
    }

    [Fact]
    public async Task GetIssues_ReturnsIssues()
    {
        // Arrange
        var mockIssues = new[]
        {
            new { id = "1", key = "TEST-1" },
            new { id = "2", key = "TEST-2" }
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"{_settings.Value.BaseUrl}/rest/api/3/search")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new { issues = mockIssues }))
            });

        // Act
        var result = await _jiraService.GetIssues();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, issue => issue.Key == "TEST-1");
        Assert.Contains(result, issue => issue.Key == "TEST-2");
    }
    
    [Fact]
    public async Task GetIssues_ReturnsIssues_WhenValidResponse()
    {
        // Arrange
        var mockResponse = new
        {
            issues = new[]
            {
                new { id = "1", key = "TEST-1" },
                new { id = "2", key = "TEST-2" }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _jiraService.GetIssues();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, issue => issue.Key == "TEST-1");
        Assert.Contains(result, issue => issue.Key == "TEST-2");
    }

    [Fact]
    public async Task GetIssues_ReturnsEmptyList_WhenNoIssues()
    {
        // Arrange
        var mockResponse = new { issues = new object[0] };

        SetupHttpResponse(HttpStatusCode.OK, mockResponse);

        // Act
        var result = await _jiraService.GetIssues();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetIssues_ThrowsException_OnUnauthorized()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, null);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _jiraService.GetIssues());
    }

    [Fact]
    public async Task GetIssues_ThrowsException_OnInternalServerError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, null);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _jiraService.GetIssues());
    }

    [Fact]
    public async Task GetIssues_HandlesMalformedResponse()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("INVALID_JSON") // Simulate malformed JSON response
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JsonReaderException>(() => _jiraService.GetIssues());

        // Assert: Validate the exception message (generalized to avoid specific parsing message dependencies)
        Assert.Contains("parsing", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsExpectedRequest(HttpRequestMessage req)
    {
        var uri = req.RequestUri?.ToString() ?? string.Empty;
        return req.Method == HttpMethod.Get && uri == $"{_settings.Value.BaseUrl}/rest/api/3/search";
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? responseContent)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => IsExpectedRequest(req)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonConvert.SerializeObject(responseContent))
            });
    }

}
