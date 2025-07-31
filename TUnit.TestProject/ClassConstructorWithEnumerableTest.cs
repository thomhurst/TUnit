using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[ClassConstructor<DependencyInjectionClassConstructor>]
[NotInParallel]
public sealed class ClassConstructorWithEnumerableTest(DummyReferenceTypeClass dummy) : IDisposable
{
    private bool _isDisposed;

    [Before(Test)]
    public void Setup()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ClassConstructorWithEnumerableTest));
        }
    }

    [Test]
    [MethodDataSource(nameof(GetValues))]
    public void DoSomething(int value)
    {
        if (dummy is null)
        {
            throw new InvalidOperationException("Dummy object was not injected");
        }
    }

    public static IEnumerable<int> GetValues() => [1, 2, 3, 4];

    public void Dispose()
    {
        _isDisposed = true;
    }
}
