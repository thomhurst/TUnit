namespace TUnit.TestProject.Bugs._7662;

public abstract class NonGenericBase
{
    [Test]
    public void InheritedTest() { }
}

[InheritsTests]
[GenerateGenericTest(typeof(int))]
public class InheritedOnly<T> : NonGenericBase;

public abstract class GenericBase<T>
{
    [Test]
    public async Task InheritedTest()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
    }
}

[InheritsTests]
[GenerateGenericTest(typeof(int))]
public class InheritedAndDeclared<T> : GenericBase<T>
{
    [Test]
    public void DeclaredTest() { }
}

[GenerateGenericTest(typeof(int))]
public class WithoutInheritance<T> : NonGenericBase
{
    [Test]
    public void DeclaredTest() { }
}

public abstract class HiddenTestBase<T>
{
    [Test]
    public void Example() => throw new InvalidOperationException("The hidden test must not execute.");

    [Test]
    [Arguments(1)]
    public void Overload(int value) { }
}

[InheritsTests]
[GenerateGenericTest(typeof(int))]
public class HidesInheritedTest<T> : HiddenTestBase<T>
{
    [Test]
    public new void Example() { }

    [Test]
    public void Overload() { }
}

public abstract class VirtualTestBase<T>
{
    [Test]
    public virtual Task Example() => throw new InvalidOperationException("The override must execute instead.");
}

[InheritsTests]
[GenerateGenericTest(typeof(int))]
public class OverridesInheritedTest<T> : VirtualTestBase<T>
{
    private int _executions;

    [Test]
    public override async Task Example()
    {
        await Assert.That(++_executions).IsEqualTo(1);
    }
}

public abstract class OverloadTestBase<T>
{
    [Test]
    [Arguments(1)]
    public void Example(T value) { }

    [Test]
    [GenerateGenericTest(typeof(int))]
    public void DifferentArity<U>() { }
}

[InheritsTests]
[GenerateGenericTest(typeof(int))]
public class RetainsInheritedOverloads<T> : OverloadTestBase<T>
{
    [Test]
    [Arguments("value")]
    public void Example(string value) { }

    [Test]
    public void DifferentArity() { }
}

public abstract class CoincidingOverloadBase<T>
{
    [Test]
    [Arguments("base")]
    public async Task Example(T value)
    {
        await Assert.That(value).IsEqualTo((T)(object)"base");
    }
}

[InheritsTests]
[GenerateGenericTest(typeof(string))]
public class RetainsCoincidingOverloads<T> : CoincidingOverloadBase<T>
{
    [Test]
    [Arguments("derived")]
    public async Task Example(string value)
    {
        await Assert.That(value).IsEqualTo("derived");
    }
}

[GenerateGenericTest(typeof(int))]
public class RetainsLiteralArityNames<T>
{
    public void Foo() { }

    [Test]
    [GenerateGenericTest(typeof(int))]
    public void Foo<U>() { }

    [Test]
    public void Foo_Generic1() { }
}

[InheritsTests]
public class NonGenericHidesInheritedTest : HiddenTestBase<int>
{
    [Test]
    public new void Example() { }

    [Test]
    public void Overload() { }
}

[InheritsTests]
[GenerateGenericTest(typeof(int))]
public class OverridesWithoutTestAttribute<T> : VirtualTestBase<T>
{
    public override Task Example() => throw new InvalidOperationException("An override without [Test] must not be discovered.");

    [Test]
    public void DeclaredTest() { }
}

[InheritsTests]
public class NonGenericOverridesWithoutTestAttribute : VirtualTestBase<int>
{
    public override Task Example() => throw new InvalidOperationException("An override without [Test] must not be discovered.");

    [Test]
    public void DeclaredTest() { }
}
