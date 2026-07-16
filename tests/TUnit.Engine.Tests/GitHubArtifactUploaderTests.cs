using Shouldly;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Tests;

public class GitHubArtifactUploaderTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ComputeExpiresAt_Returns_Null_When_No_Retention_Requested()
    {
        GitHubArtifactUploader.ComputeExpiresAt(null, maxRetentionDays: 90, FixedUtcNow).ShouldBeNull();
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public void ComputeExpiresAt_Returns_Null_For_NonPositive_Retention(int retentionDays)
    {
        GitHubArtifactUploader.ComputeExpiresAt(retentionDays, maxRetentionDays: 90, FixedUtcNow).ShouldBeNull();
    }

    [Test]
    public void ComputeExpiresAt_Returns_Rfc3339_Timestamp_Offset_From_Now()
    {
        var result = GitHubArtifactUploader.ComputeExpiresAt(5, maxRetentionDays: null, FixedUtcNow);

        result.ShouldBe("2026-06-22T12:00:00Z");
    }

    [Test]
    public void ComputeExpiresAt_Clamps_To_Repository_Maximum_When_Exceeded()
    {
        var result = GitHubArtifactUploader.ComputeExpiresAt(120, maxRetentionDays: 90, FixedUtcNow);

        // 90 days, not 120
        result.ShouldBe("2026-09-15T12:00:00Z");
    }

    [Test]
    public void ComputeExpiresAt_Does_Not_Clamp_When_Within_Repository_Maximum()
    {
        var result = GitHubArtifactUploader.ComputeExpiresAt(7, maxRetentionDays: 90, FixedUtcNow);

        result.ShouldBe("2026-06-24T12:00:00Z");
    }

    [Test]
    public void ComputeExpiresAt_Ignores_NonPositive_Repository_Maximum()
    {
        var result = GitHubArtifactUploader.ComputeExpiresAt(10, maxRetentionDays: 0, FixedUtcNow);

        result.ShouldBe("2026-06-27T12:00:00Z");
    }
}
