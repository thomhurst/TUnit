namespace TUnit.Core;

public record struct SourceGeneratedTestNode
{
    public FailedInitializationTest? FailedInitializationTest { get; }
    public TestMetadata? TestMetadata { get; }

    public SourceGeneratedTestNode(FailedInitializationTest failedInitializationTest)
    {
        FailedInitializationTest = failedInitializationTest;
    }
    
    public SourceGeneratedTestNode(TestMetadata testMetadata)
    {
        TestMetadata = testMetadata;
    }
    
    public static implicit operator SourceGeneratedTestNode(FailedInitializationTest failedInitializationTest) => new(failedInitializationTest);
    public static implicit operator SourceGeneratedTestNode(TestMetadata testMetadata) => new(testMetadata);
}