using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Models.Messaging;

namespace SVC_Scheduler.Tests.Integration;

public abstract class BaseSchedulerIntegrationTest(CustomWebApplicationFactory factory)
    : IAsyncLifetime
{
    private protected CustomWebApplicationFactory Factory { get; } = factory;

    private protected HttpClient HttpClient { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        HttpClient = Factory.CreateClient();
        await ClearWireMockMappings();
        Factory.ClearPublishedMessages();
    }

    public virtual async Task DisposeAsync()
    {
        await ClearWireMockMappings();
        Factory.ClearPublishedMessages();
        HttpClient.Dispose();
    }

    private async Task ClearWireMockMappings()
    {
        await Factory.SvcExternalServerMock.ResetMappingsAsync();
        await Factory.SvcBridgeServerMock.ResetMappingsAsync();
    }

    protected T GetRequiredService<T>()
        where T : notnull
    {
        return Factory.Services.GetRequiredService<T>();
    }

    protected async Task<bool> WaitForJobCompletionMessageAsync(
        string expectedJobName,
        bool expectedSuccess = true,
        TimeSpan? timeout = null
    )
    {
        return await Factory.WaitForMessageAsync(
            message => message.JobName == expectedJobName && message.Success == expectedSuccess,
            timeout
        );
    }

    protected async Task<bool> WaitForCacheWarmupMessageAsync(TimeSpan? timeout = null)
    {
        return await Factory.WaitForMessageAsync(
            message => message.JobName.Contains("Cache Warmup") && message.Success,
            timeout
        );
    }

    protected JobCompletedMessage? GetPublishedMessage(string jobName, bool success = true)
    {
        return Factory.PublishedMessages.FirstOrDefault(m =>
            m.JobName == jobName && m.Success == success
        );
    }

    protected void AssertNoMessagesPublished()
    {
        Factory.PublishedMessages.Should().BeEmpty();
    }

    protected void AssertMessagePublished(string expectedJobName, bool expectedSuccess = true)
    {
        var message = GetPublishedMessage(expectedJobName, expectedSuccess);
        message
            .Should()
            .NotBeNull(
                $"Expected message for job '{expectedJobName}' with success={expectedSuccess}"
            );
    }
}
