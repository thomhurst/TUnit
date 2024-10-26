using Microsoft.Extensions.DependencyInjection;

namespace TUnit.TestProject;

[ClassConstructor<DependencyInjectionClassConstructor>]
[NotInParallel]
public sealed class ClassConstructorWithEnumerableTest(IServiceProvider services) : IDisposable
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
        ActivatorUtilities.GetServiceOrCreateInstance<DummyReferenceTypeClass>(services);
    }

    public static IEnumerable<int> GetValues() => [1, 2, 3, 4];

    public void Dispose()
    {
        _isDisposed = true;
    }
}