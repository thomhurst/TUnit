namespace TUnit.TestProject.Bugs._7490;

[Arguments(42)]
[Arguments("base")]
public abstract class ArgumentsBase;

public abstract class IntermediateBase : ArgumentsBase;

[Arguments(true)]
public class InheritedArguments<T>(T value) : IntermediateBase
{
    [Test]
    public void Test() => Verify();

    [Test]
    [Arguments(7)]
    [Arguments("method")]
    public void GenericMethod<TMethod>(TMethod methodValue)
    {
        Verify();
        if (methodValue is not (7 or "method"))
        {
            throw new InvalidOperationException("Expected the method argument row.");
        }
    }

    private void Verify()
    {
        if (value is not (42 or "base" or true))
        {
            throw new InvalidOperationException("Expected a direct or inherited constructor argument row.");
        }
    }
}

public class InheritedOnlyArguments<T>(T value) : IntermediateBase
{
    [Test]
    public void Test()
    {
        if (value is not (42 or "base"))
        {
            throw new InvalidOperationException("Expected an inherited constructor argument row.");
        }
    }
}

[Arguments("base")]
public abstract class StringArgumentsBase;

public class InheritedNullMethodArguments<T>(T value) : StringArgumentsBase
    where T : class, IComparable
{
    [Test]
    [Arguments(null)]
    public void Test(T? methodValue)
    {
        if (value is not "base" || methodValue is not null)
        {
            throw new InvalidOperationException("Expected the inherited string row with a null method argument.");
        }
    }
}

[GenerateGenericTest(typeof(string))]
public abstract class GenericTestBase<T>;

public class InheritedGenericTestNullArguments<T> : GenericTestBase<T>
{
    [Test]
    [Arguments(null)]
    public void Test(T? value)
    {
        if (typeof(T) != typeof(object) || value is not null)
        {
            throw new InvalidOperationException("Expected null method arguments to infer object independently of inherited generic test attributes.");
        }
    }
}

[Arguments(42)]
[MethodDataSource(typeof(InheritedMixedSourcesBase), nameof(StringRows))]
public abstract class InheritedMixedSourcesBase
{
    public static IEnumerable<string> StringRows() => ["constructor"];
}

public class InheritedMixedSources<T>(T value) : InheritedMixedSourcesBase
{
    [Test]
    [Arguments("method")]
    public void Test(T methodValue)
    {
        if (value is not "constructor" || methodValue is not "method")
        {
            throw new InvalidOperationException("Expected the inherited method source to supply the constructor row.");
        }
    }
}

[MethodDataSource(nameof(Rows))]
public abstract class InheritedNullSourceBase
{
    public static IEnumerable<string> Rows() => ["constructor"];
}

public class InheritedSourceNullArguments<T>(T value) : InheritedNullSourceBase
{
    [Test]
    [Arguments(null)]
    public void Test(T? methodValue)
    {
        if (typeof(T) != typeof(string) || value is not "constructor" || methodValue is not null)
        {
            throw new InvalidOperationException("Expected the inherited source to infer string and supply the constructor row.");
        }
    }
}

[MethodDataSource(typeof(ExternalComparableRows), nameof(ExternalComparableRows.Rows))]
public abstract class ExternalComparableSourceBase;

public static class ExternalComparableRows
{
    public static IEnumerable<IComparable> Rows() => ["constructor"];
}

[GenerateGenericTest(typeof(string))]
public class InheritedExternalSource<T>(T value) : ExternalComparableSourceBase
{
    [Test]
    public void Test()
    {
        if (typeof(T) != typeof(string) || value is not "constructor")
        {
            throw new InvalidOperationException("External sources must use row values rather than their declared interface type.");
        }
    }
}

[GenerateGenericTest(typeof(string))]
public class InheritedConstrainedExternalSource<T>(T value) : ExternalComparableSourceBase
    where T : class, IComparable<string>
{
    [Test]
    public void Test()
    {
        if (value.CompareTo("constructor") != 0)
        {
            throw new InvalidOperationException("Expected the valid explicit string instantiation without a declared-interface constraint failure.");
        }
    }
}

[MethodDataSource(typeof(PrivateRowSourceBase), "Rows")]
public abstract class PrivateRowSourceBase
{
    private sealed class PrivateRow;
    private static IEnumerable<PrivateRow> Rows() => [new()];
}

[GenerateGenericTest(typeof(object))]
public class InheritedPrivateRowSource<T>(T value) : PrivateRowSourceBase
{
    [Test]
    public void Test()
    {
        if (value is null)
        {
            throw new InvalidOperationException("Expected the private external row to remain runnable.");
        }
    }
}
