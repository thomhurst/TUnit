[assembly: TUnit.Core.Category("Issue7437Assembly")]
[assembly: TUnit.Core.Property("Issue7437Scope", "Assembly")]

namespace TUnit.TestProject.Bugs._7437;

[Category("Issue7437Base")]
[Property("Issue7437Scope", "Base")]
public abstract class FilteredFailureBase;

[GenerateGenericTest(typeof(string))]
[Category("Issue7437Class")]
[Property("Issue7437Scope", "Class")]
public class InvalidClass<T> : FilteredFailureBase where T : struct
{
    [Test]
    [Category("Issue7437Method")]
    [Property("Issue7437Scope", "Method")]
    public void Run() { }
}

[Category("Issue7437Class")]
[Property("Issue7437Scope", "Class")]
public class InvalidMethod : FilteredFailureBase
{
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Category("Issue7437Method")]
    [Property("Issue7437Scope", "Method")]
    public void GenericRun<T>() where T : struct { }
}

[MethodDataSource(typeof(UntypedSource), nameof(UntypedSource.Rows))]
[Category("Issue7437Class")]
[Property("Issue7437Scope", "Class")]
public class UninferredClass<T>(T value) : FilteredFailureBase
{
    public T Value { get; } = value;

    [Test]
    [Category("Issue7437Method")]
    [Property("Issue7437Scope", "Method")]
    public void Run() { }
}

public static class UntypedSource
{
    public static IEnumerable<object> Rows() => [new object()];
}

// This class has no executable instantiations: the top-level generated source cannot name
// the private filter attribute, but its actual constructor still supplies the category.
public class PrivateAttributeContainer
{
    private sealed class Hidden;
    private sealed class PrivateCategoryAttribute<T>() : CategoryAttribute($"Issue7437Private{typeof(T).Name}");

    [PrivateCategory<int>]
    [PrivateCategory<string>]
    [GenerateGenericTest(typeof(Hidden))]
    public class InvalidPrivateAttributeClass<T> where T : struct
    {
        [Test]
        [PrivateCategory<double>]
        [Arguments("invalid")]
        public void Run(T value) { }

        // These unannotated overloads must never supply the test method's attributes.
        public void Run(string value) { }
        public void Run(object value) { }
    }
}

[ClassConstructor<ThrowingClassConstructor>]
[GenerateGenericTest(typeof(string))]
[GenerateGenericTest(typeof(object))]
[Category("Issue7437Constructor")]
public class InvalidConstructorClass<T> where T : struct
{
    [Test]
    public void Run() { }
}

public sealed class ThrowingClassConstructor : TUnit.Core.Interfaces.IClassConstructor
{
    public ThrowingClassConstructor() => throw new InvalidOperationException("Unused class constructor initialized");

    public Task<object> Create(
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        ClassConstructorMetadata classConstructorMetadata) => throw new InvalidOperationException("Unused class constructor invoked");
}

[GenerateGenericTest(typeof(string))]
[Category("Issue7437UnusedSource")]
public class InvalidUnusedSourceClass<T> where T : struct
{
    [Test]
    [UnusedSource]
    public void Run() { }
}

public sealed class UnusedSourceAttribute : Attribute, IDataSourceAttribute
{
    [ClassDataSource<ThrowingDependency>]
    public ThrowingDependency Dependency { get; set; } = null!;

    public bool SkipIfEmpty { get; set; }
    public bool DeferEnumeration { get; set; }
    public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        => throw new InvalidOperationException("Unused data source invoked");
}

public sealed class ThrowingDependency
{
    public ThrowingDependency() => throw new InvalidOperationException("Unused data source property injected");
}
