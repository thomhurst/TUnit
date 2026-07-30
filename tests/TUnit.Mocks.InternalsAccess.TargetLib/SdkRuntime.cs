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
/// An internal generic interface: closed instantiations must be mockable too.
/// </summary>
internal interface IInternalRepository<T>
{
    T Load(int id);
}

/// <summary>
/// An internal class with virtual members and an internal constructor — the partial-mock shape.
/// </summary>
internal class InternalWidget
{
    internal InternalWidget()
    {
    }

    public virtual string Name => "real-widget";

    public virtual int Weight() => 100;
}

/// <summary>
/// Simulates SDK-internal call sites the test has no control over.
/// </summary>
public static class SdkRuntime
{
    public static string DescribeRepository(IFeatureCollection features)
    {
        var repository = features.Get<IInternalRepository<string>>();
        return repository is null ? "<missing>" : repository.Load(1);
    }

    public static string DescribeWidget(IFeatureCollection features)
    {
        var widget = features.Get<InternalWidget>();
        return widget is null ? "<missing>" : $"{widget.Name}:{widget.Weight()}";
    }

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
