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
