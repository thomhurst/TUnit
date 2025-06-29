using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Test metadata source for source-generated tests
/// </summary>
public sealed class SourceGeneratedTestMetadataSource : ITestMetadataSource
{
    private readonly Func<List<TestMetadata>> _getMetadata;
    
    public SourceGeneratedTestMetadataSource(Func<List<TestMetadata>> getMetadata)
    {
        _getMetadata = getMetadata ?? throw new ArgumentNullException(nameof(getMetadata));
    }
    
    public Task<IEnumerable<TestMetadata>> GetTestMetadata()
    {
        return Task.FromResult<IEnumerable<TestMetadata>>(_getMetadata());
    }
}