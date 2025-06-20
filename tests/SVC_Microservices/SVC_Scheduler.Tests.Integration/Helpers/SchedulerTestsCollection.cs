namespace SVC_Scheduler.Tests.Integration.Helpers;

/// <summary>
/// Test collection for scheduler integration tests.
/// </summary>
[CollectionDefinition("Scheduler Integration Tests")]
public class SchedulerTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
