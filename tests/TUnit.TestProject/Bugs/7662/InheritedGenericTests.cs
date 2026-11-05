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
