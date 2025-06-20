namespace TUnit.Core;

/// <summary>
/// Internal interface for TestBuilder implementations.
/// </summary>
internal interface ITestBuilderInternal
{
    Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default);
}

/// <summary>
/// Adapter to unify different TestBuilder implementations.
/// </summary>
internal class TestBuilderAdapter : ITestBuilderInternal
{
    private readonly TestBuilder? _basic;
    private readonly TestBuilderOptimized? _optimized;
    private readonly TestBuilderWithDiagnostics? _withDiagnostics;
    
    public TestBuilderAdapter(TestBuilder basic)
    {
        _basic = basic;
    }
    
    public TestBuilderAdapter(TestBuilderOptimized optimized)
    {
        _optimized = optimized;
    }
    
    public TestBuilderAdapter(TestBuilderWithDiagnostics withDiagnostics)
    {
        _withDiagnostics = withDiagnostics;
    }
    
    public Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (_basic != null)
            return _basic.BuildTestsAsync(metadata, cancellationToken);
        
        if (_optimized != null)
            return _optimized.BuildTestsAsync(metadata, cancellationToken);
        
        if (_withDiagnostics != null)
            return _withDiagnostics.BuildTestsAsync(metadata, cancellationToken);
        
        throw new InvalidOperationException("No TestBuilder implementation available");
    }
}