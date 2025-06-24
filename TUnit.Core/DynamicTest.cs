namespace TUnit.Core;

/// <summary>
/// Represents a test discovery result
/// </summary>
public class DiscoveryResult
{
    public static DiscoveryResult Empty => new DiscoveryResult();
}

/// <summary>
/// Base class for dynamic tests
/// </summary>
public abstract class DynamicTest
{
    public abstract IEnumerable<DiscoveryResult> GetTests();
}

/// <summary>
/// Generic dynamic test
/// </summary>
public abstract class DynamicTest<T> : DynamicTest where T : class
{
}

/// <summary>
/// Interface for dynamic test sources
/// </summary>
public interface IDynamicTestSource
{
    IEnumerable<DynamicTest> GetTests();
}