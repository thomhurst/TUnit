namespace TUnit.Core;

internal interface ITUnitMessageBus
{
    Task Discovered(TestContext testContext);
    Task InProgress(TestContext testContext);
    Task Passed(TestContext testContext, DateTimeOffset start);
    Task Failed(TestContext testContext, Exception exception, DateTimeOffset? start = null);
    Task FailedInitialization(FailedInitializationTest failedInitializationTest);
    Task Skipped(TestContext testContext, string reason);
    Task Cancelled(TestContext testContext);
    
    Task SessionArtifact(Artifact artifact);
    Task TestArtifact(TestContext testContext, Artifact artifact);
}