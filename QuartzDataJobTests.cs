using System.Threading.Tasks;
using JiraApiProject.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class QuartzDataJobTests
{
    [Fact]
    public async Task Execute_ShouldCallPopulateData()
    {
        // Arrange
        var mockJiraService = new Mock<IJiraService>();
        var mockLogger = new Mock<ILogger<QuartzDataJob>>();
        var jiraSettings = Options.Create(new JiraSettings
        {
            BaseUrl = "https://example.atlassian.net",
            Username = "testuser",
            ApiToken = "apitoken"
        });

        mockJiraService
            .Setup(s => s.PopulateData(It.IsAny<JiraCredentials>()))
            .Returns(Task.CompletedTask);

        var job = new QuartzDataJob(mockJiraService.Object, mockLogger.Object, jiraSettings);

        var context = Mock.Of<Quartz.IJobExecutionContext>();

        // Act
        await job.Execute(context);

        // Assert
        mockJiraService.Verify(
            s => s.PopulateData(It.IsAny<JiraCredentials>()),
            Times.Once
        );
    }

}
