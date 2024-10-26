namespace TUnit.Core;

internal interface ITUnitMessageBus
{
    ValueTask Discovered(TestContext testContext);
    ValueTask InProgress(TestContext testContext);
    ValueTask Passed(TestContext testContext, DateTimeOffset start);
    ValueTask Failed(TestContext testContext, Exception exception, DateTimeOffset start);
    ValueTask FailedInitialization(FailedInitializationTest failedInitializationTest);
    ValueTask Skipped(TestContext testContext, string reason);
    ValueTask Cancelled(TestContext testContext);
    
    ValueTask SessionArtifact(Artifact artifact);
    ValueTask TestArtifact(TestContext testContext, Artifact artifact);
}