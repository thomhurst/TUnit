using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

[ClassConstructor<DependencyInjectionClassConstructor>]
public class ClassConstructorTest(SomeAsyncDisposableClass someAsyncDisposableClass)
{
    [Test]
    public void Test()
    {
    }
}