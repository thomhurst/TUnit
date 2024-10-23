namespace TUnit.Core;

internal interface ITUnitMessageBus
{
    Task Discovered(TestContext testContext);
    Task InProgress(TestContext testContext);
    Task Passed(TestContext testContext);
    Task Failed(TestContext testContext, Exception exception);
    Task FailedInitialization(FailedInitializationTest failedInitializationTest);
    Task Errored(TestContext testContext, Exception exception);
    Task Skipped(TestContext testContext, string reason);
    Task Cancelled(TestContext testContext, Exception exception);
    
    Task Artifact(Artifact artifact);
}