namespace TUnit.Mocks.SourceGenerator;

/// <summary>
/// Names of the <see cref="MockGenerator"/> pipeline steps, for incrementality tests.
/// </summary>
internal static class MockTrackingNames
{
    public const string MockOfInvocations = nameof(MockOfInvocations);
    public const string GenerateMockAttributes = nameof(GenerateMockAttributes);
    public const string MockExtensionInvocations = nameof(MockExtensionInvocations);
    public const string DistinctRequests = nameof(DistinctRequests);
    public const string DistinctModels = nameof(DistinctModels);
    public const string EmitResults = nameof(EmitResults);
}
