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
