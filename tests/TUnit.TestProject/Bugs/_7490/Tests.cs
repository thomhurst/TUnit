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

[Arguments(42)]
[MethodDataSource(nameof(Rows))]
public abstract class InheritedDeclaredRowBase
{
    public static IEnumerable<IComparable> Rows() => ["constructor"];
}

public class InheritedDeclaredRowPartition<T>(T value) : InheritedDeclaredRowBase
{
    [Test]
    public void Test()
    {
        if ((typeof(T) != typeof(int) || value is not 42)
            && (typeof(T) != typeof(IComparable) || value is not "constructor"))
        {
            throw new InvalidOperationException("Expected constructor rows to retain their inferred declared types.");
        }
    }
}


[Arguments(42)]
[MethodDataSource(nameof(Rows))]
public class DirectDeclaredRowPartition<T>(T value)
{
    public static IEnumerable<IComparable> Rows() => [43];

    [Test]
    public void Test()
    {
        if ((typeof(T) != typeof(int) || value is not 42)
            && (typeof(T) != typeof(IComparable) || value is not 43))
        {
            throw new InvalidOperationException("Expected direct member rows to retain their declared interface type.");
        }
    }
}

[MethodDataSource(nameof(Rows), SkipIfEmpty = true)]
public abstract class InheritedEmptyDeclaredRowBase
{
    public static IEnumerable<string> Rows() => [];
}

[Arguments(42)]
public class InheritedEmptyDeclaredRowPartition<T>(T value) : InheritedEmptyDeclaredRowBase
{
    [Test]
    public void Test()
    {
        if (typeof(T) != typeof(int) || value is not 42)
        {
            throw new InvalidOperationException("An empty inherited source must not fabricate a constructor row.");
        }
    }
}

[Arguments(42)]
[MethodDataSource(nameof(Rows), SkipIfEmpty = true)]
public class DirectEmptyDeclaredRowPartition<T>(T value)
{
    public static IEnumerable<string> Rows() => [];

    [Test]
    public void Test()
    {
        if (typeof(T) != typeof(int) || value is not 42)
        {
            throw new InvalidOperationException("An empty direct source must not fabricate a constructor row.");
        }
    }
}

[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(ConstructorRows))]
public class DirectMemberMethodRows<T>(T constructorValue)
{
    public static IEnumerable<int> ConstructorRows() => [42];
    public static IEnumerable<int> MethodRows() => [43];

    [Test]
    [MethodDataSource(nameof(MethodRows))]
    public void Test(T methodValue)
    {
        if (constructorValue is not 42 || methodValue is not 43)
        {
            throw new InvalidOperationException("Expected separate constructor and method rows.");
        }
    }
}

[MethodDataSource(nameof(ConstructorRows))]
public abstract class InheritedMemberMethodRowsBase
{
    public static IEnumerable<string> ConstructorRows() => ["constructor"];
    public static IEnumerable<string> MethodRows() => ["method"];
}

public class InheritedMemberMethodRows<T>(T constructorValue) : InheritedMemberMethodRowsBase
{
    [Test]
    [MethodDataSource(nameof(MethodRows))]
    public void Test(T methodValue)
    {
        if (constructorValue is not "constructor" || methodValue is not "method")
        {
            throw new InvalidOperationException("Expected inherited constructor and method providers to remain separate.");
        }
    }
}

public abstract class InheritedOpenMethodRowsBase<U>
{
    public static IEnumerable<List<U>> Rows() => [new()];
}

[GenerateGenericTest(typeof(int))]
public class InheritedOpenMethodRows<T> : InheritedOpenMethodRowsBase<T>
{
    [Test]
    [MethodDataSource(nameof(Rows))]
    public void Test(List<T> values)
    {
        if (typeof(T) != typeof(int) || values.Count != 0)
        {
            throw new InvalidOperationException("Expected the explicit closed class type to supply inherited method rows.");
        }
    }
}

public static class ExternalMethodRowsProvider
{
    public static IEnumerable<int> Rows() => [43];
}

[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(ConstructorRows))]
public class ExternalMemberMethodRows<T>(T constructorValue)
{
    public static IEnumerable<int> ConstructorRows() => [42];

    [Test]
    [MethodDataSource(typeof(ExternalMethodRowsProvider), nameof(ExternalMethodRowsProvider.Rows))]
    public void Test(T methodValue)
    {
        if (constructorValue is not 42 || methodValue is not 43)
        {
            throw new InvalidOperationException("Expected the external method provider on the constructor-scoped entry.");
        }
    }
}

[Arguments(42)]
[GenerateGenericTest(typeof(IComparable))]
[MethodDataSource(nameof(Rows), SkipIfEmpty = true)]
public class ExplicitEmptyDeclaredRowPartition<T>(T value)
{
    public static IEnumerable<IComparable> Rows() => [];

    [Test]
    public void Test()
    {
        if (value is not 42 || (typeof(T) != typeof(int) && typeof(T) != typeof(IComparable)))
        {
            throw new InvalidOperationException("Expected the explicit interface instantiation to retain its assignable constructor row.");
        }
    }
}

[Arguments(42)]
[GenerateGenericTest(typeof(int))]
[MethodDataSource(nameof(Rows))]
public class ExplicitMatchingLiteralRows<T>(T value)
{
    public static IEnumerable<int> Rows() => [43];

    [Test]
    public void Test()
    {
        if (typeof(T) != typeof(int) || value is not (42 or 43))
        {
            throw new InvalidOperationException("Expected both matching literal and declared constructor rows.");
        }
    }
}

public class TypedMethodRowsAttribute : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata) => [() => 43];
}

[MethodDataSource(nameof(ConstructorRows))]
public class ConstructorScopedTypedMethodRows<T>(T constructorValue)
{
    public static IEnumerable<int> ConstructorRows() => [42];

    [Test]
    [TypedMethodRows]
    public void Test(T methodValue)
    {
        if (constructorValue is not 42 || methodValue is not 43)
        {
            throw new InvalidOperationException("Expected typed method rows on the constructor-scoped entry.");
        }
    }
}
