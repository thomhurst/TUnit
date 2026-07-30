namespace FakeSdk;

/// <summary>
/// Mirrors the shape from issue #6514: a public generic accessor whose type arguments are chosen
/// by SDK-internal code, using types the consuming assembly cannot normally name.
/// </summary>
public interface IFeatureCollection
{
    T Get<T>();
}

/// <summary>
/// The unnameable type. Internal, and this assembly grants no InternalsVisibleTo whatsoever.
/// </summary>
internal interface IInternalBindingsFeature
{
    string InvocationResult { get; set; }

    int Compute(int seed);
}

/// <summary>
/// Simulates SDK-internal call sites the test has no control over.
/// </summary>
public static class SdkRuntime
{
    public static string DescribeInvocation(IFeatureCollection features)
    {
        var bindings = features.Get<IInternalBindingsFeature>();
        return bindings is null ? "<missing>" : bindings.InvocationResult;
    }

    public static int RunComputation(IFeatureCollection features, int seed)
    {
        var bindings = features.Get<IInternalBindingsFeature>();
        return bindings?.Compute(seed) ?? -1;
    }
}
