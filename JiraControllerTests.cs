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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private Mock<ILogger<JiraService>> _mockLogger;
    private ILogger<JiraService> _logger;
    private readonly Mock<ILogger<JiraController>> _mockLoggerController; // New logger for the controller

    public JiraControllerTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockLogger = new Mock<ILogger<JiraService>>();
        _logger = _mockLogger.Object; // Extract the mocked logger object
        // Mock logger for the controller
        _mockLoggerController = new Mock<ILogger<JiraController>>();
        _settings = new JiraSettings
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            ApiToken = "apitoken"
        };
        _mockSettings = new Mock<IOptions<JiraSettings>>();
        _mockSettings.Setup(s => s.Value).Returns(_settings);

        var jiraService = new JiraService(_httpClient, _mockSettings.Object, _logger);
        _jiraController = new JiraController(jiraService, _mockLoggerController.Object);
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
    // Arrange
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
            ItExpr.IsAny<CancellationToken>()
        )
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

    var jiraService = new JiraService(httpClient, options, Mock.Of<ILogger<JiraService>>());
    var jiraController = new JiraController(jiraService, Mock.Of<ILogger<JiraController>>());

    // Act
    var result = await jiraController.GetIssues();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result); // Ensure the result is OkObjectResult
    var data = Assert.IsAssignableFrom<List<Issue>>(okResult.Value); // Ensure the returned data is a List<Issue>

    Assert.Equal(2, data.Count); // Validate the number of issues
    Assert.Contains(data, issue => issue.Key == "TEST-1");
    Assert.Contains(data, issue => issue.Key == "TEST-2");
}

        [Fact]
        public async Task GetIssues_ReturnsNotFound_WhenServiceReturnsEmptyList()
        {
            SetupHttpResponseForIssues(Enumerable.Empty<object>()); // Simulate an empty list response

            var result = await _jiraController.GetIssues();
            Assert.IsType<NotFoundResult>(result); // Expect NotFoundResult for an empty list
        }


        [Fact]
        public async Task GetIssues_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange: Mock the service response to simulate a null issues list
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"issues\": null}") // Simulate API returning null issues
                });

            // Act: Call the controller's GetIssues method
            var result = await _jiraController.GetIssues();

            // Assert: Verify that the controller returns a NotFoundResult
            Assert.IsType<NotFoundResult>(result);
        }



    [Fact]
    public async Task GetIssues_ReturnsInternalServerError_WhenServiceThrowsException()
    {
        SetupHttpErrorResponse(HttpStatusCode.InternalServerError);

        var result = await _jiraController.GetIssues();
        var statusCodeResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetIssues_HandlesMalformedResponse()
    {
        // Arrange
        SetupMalformedHttpResponse();

        // Act
        var result = await _jiraController.GetIssues();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Malformed JSON response.", badRequestResult.Value);
    }


}
