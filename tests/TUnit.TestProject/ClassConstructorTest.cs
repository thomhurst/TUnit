namespace TUnit.TestProject;

[ClassConstructor<DependencyInjectionClassConstructor>]
public class ClassConstructorTest(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public DummyReferenceTypeClass DummyReferenceTypeClass { get; } = dummyReferenceTypeClass;

    [Test]
    public void Test()
    {
    }
}
