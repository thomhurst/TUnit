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

    [PrivateCategory<int>]
    [GenerateGenericTest(typeof(Hidden))]
    public class InvalidPrivateArrayClass<T> where T : struct
    {
        // Put the concrete overload first so a name/count fallback cannot pass accidentally.
        public void Run(string[] value) { }

        [Test]
        [PrivateCategory<double>]
        [Arguments(null)]
        public void Run(T[] value) { }
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

// Deliberately vary inherited category usage to exercise reflection inheritance parity.
#pragma warning disable TUnit0028
public class InheritedPrivateAttributeContainer
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    private sealed class NonInheritedCategoryAttribute() : CategoryAttribute("Issue7437NotInherited");

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    private sealed class SingleCategoryAttribute(string category) : CategoryAttribute(category);

    // No own AttributeUsage: runtime defaults apply, even though Category allows multiples.
    private sealed class ImplicitCategoryAttribute(string category) : CategoryAttribute(category);

    [ImplicitCategory("Issue7437ImplicitShadowed")]
    public abstract class ImplicitBase;

    [GenerateGenericTest(typeof(string))]
    [ImplicitCategory("Issue7437ImplicitDerived")]
    public class InvalidImplicitInheritedClass<T> : ImplicitBase where T : struct
    {
        [Test]
        public void Run() { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    private sealed class RepeatedCategoryAttribute(string category) : CategoryAttribute(category);

    public abstract class MethodBase
    {
        [RepeatedCategory("Issue7437InheritedMethod")]
        public virtual void Run() { }
    }

    [GenerateGenericTest(typeof(string))]
    [RepeatedCategory("Issue7437OverrideClass")]
    public class InvalidOverrideClass<T> : MethodBase where T : struct
    {
        [Test]
        public override void Run() { }
    }

    [NonInheritedCategory]
    [SingleCategory("Issue7437Shadowed")]
    public abstract class Base;

    [GenerateGenericTest(typeof(string))]
    [SingleCategory("Issue7437Derived")]
    public class InvalidInheritedClass<T> : Base where T : struct
    {
        [Test]
        public void Run() { }
    }
}

#pragma warning restore TUnit0028
