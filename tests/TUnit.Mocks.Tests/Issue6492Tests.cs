using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6492
// The generated factory dispatches between same-arity constructor overloads with `is` patterns
// built from each parameter's fully qualified type. Nullable types are never legal as the type of
// an `is` pattern (CS8116), so a constructor taking `Uri?`, a nullable delegate, or `int?` emitted
// uncompilable code. The pattern must test the underlying type; anything nullable also accepts null.

#region Test types

public delegate string NullableSerializerFactory(int seed);

public interface INullableCtorDependency
{
    string Name { get; }
}

public class NullableCtorClient
{
    // Two same-arity overloads force the type-check dispatch path (a single overload per arity
    // would be dispatched positionally without any `is` pattern).
    public NullableCtorClient(Uri? uri) => Origin = uri?.ToString() ?? "<none>";

    public NullableCtorClient(string? host) => Origin = host ?? "<none>";

    public NullableCtorClient(NullableSerializerFactory? factory, INullableCtorDependency? dependency)
        => Origin = $"{factory?.Invoke(1) ?? "<none>"}/{dependency?.Name ?? "<none>"}";

    public NullableCtorClient(int? port, INullableCtorDependency? dependency)
        => Origin = $"{port?.ToString() ?? "<none>"}/{dependency?.Name ?? "<none>"}";

    public virtual string Origin { get; }

    public virtual string Describe() => Origin;
}

#endregion

public class Issue6492Tests
{
    [Test]
    public async Task Ctor_With_Nullable_Reference_Type_Compiles_And_Dispatches()
    {
        var mock = NullableCtorClient.Mock(new Uri("http://localhost:9200"));

        await Assert.That(mock.Object.Origin).IsEqualTo("http://localhost:9200/");
    }

    [Test]
    public async Task Ctor_Overload_Dispatch_Picks_The_Matching_Nullable_Parameter_Type()
    {
        var mock = NullableCtorClient.Mock("elastic-host");

        await Assert.That(mock.Object.Origin).IsEqualTo("elastic-host");
    }

    [Test]
    public async Task Ctor_With_Nullable_Delegate_And_Interface_Parameters_Compiles()
    {
        NullableSerializerFactory factory = seed => $"serializer-{seed}";
        var dependency = INullableCtorDependency.Mock();
        dependency.Name.Returns("dep");

        var mock = NullableCtorClient.Mock(factory, dependency.Object);

        await Assert.That(mock.Object.Origin).IsEqualTo("serializer-1/dep");
    }

    [Test]
    public async Task Ctor_With_Nullable_Value_Type_Parameter_Dispatches_On_Boxed_Value()
    {
        var dependency = INullableCtorDependency.Mock();
        dependency.Name.Returns("dep");

        var mock = NullableCtorClient.Mock((int?)9200, dependency.Object);

        await Assert.That(mock.Object.Origin).IsEqualTo("9200/dep");
    }

    [Test]
    public async Task Null_Argument_Still_Selects_A_Nullable_Overload()
    {
        var mock = NullableCtorClient.Mock((Uri?)null);

        await Assert.That(mock.Object.Origin).IsEqualTo("<none>");
    }

    [Test]
    public async Task Mocked_Members_Work_On_A_Nullable_Ctor_Type()
    {
        var mock = NullableCtorClient.Mock(new Uri("http://localhost:9200"));
        mock.Describe().Returns("stubbed");

        await Assert.That(mock.Object.Describe()).IsEqualTo("stubbed");
    }
}
