using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7077;

[EngineTest(ExpectedResult.Pass)]
public class EmptySkipReasonTests
{
    [Test, Skip("")]
    public void EmptyReason_IsSkipped() => throw new Exception("test ran");

    [Test, Skip(null!)]
    public void NullReason_IsSkipped() => throw new Exception("test ran");

    [Test, Skip("reason")]
    public void NonEmptyReason_IsSkipped() => throw new Exception("control ran");

    [Test, EmptyReasonSkip]
    public void DerivedAttributeWithEmptyReason_IsSkipped() => throw new Exception("test ran");
}

[EngineTest(ExpectedResult.Pass)]
[Skip("")]
public class EmptySkipReasonClassTests
{
    public EmptySkipReasonClassTests() => throw new Exception("constructor ran");

    [Test]
    public void ClassLevelEmptyReason_IsSkipped() => throw new Exception("test ran");
}

/// <summary>
/// A derived skip attribute is evaluated at registration time rather than discovery time,
/// so it exercises the runtime skip path with an empty reason.
/// </summary>
public class EmptyReasonSkipAttribute() : SkipAttribute("")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context) => Task.FromResult(true);
}
