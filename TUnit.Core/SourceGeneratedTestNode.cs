namespace TUnit.Core;

public record SourceGeneratedTestNode
{
    private readonly FailedInitializationTest? _failedInitializationTest;
    private readonly TestMetadata? _testMetadata;

    public SourceGeneratedTestNode(FailedInitializationTest failedInitializationTest)
    {
        _failedInitializationTest = failedInitializationTest;
    }
    
    public SourceGeneratedTestNode(TestMetadata testMetadata)
    {
        _testMetadata = testMetadata;
    }
    
    public static implicit operator SourceGeneratedTestNode(FailedInitializationTest failedInitializationTest) => new(failedInitializationTest);
    public static implicit operator SourceGeneratedTestNode(TestMetadata testMetadata) => new(testMetadata);
}