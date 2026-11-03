using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7205;

public sealed class LentConstructorDependencyInjectionAttribute()
    : ProbeDependencyInjectionAttribute(nameof(LentConstructorScopeTests));

public sealed class TemporaryPropertyDependencyInjectionAttribute()
    : ProbeDependencyInjectionAttribute(nameof(LentConstructorScopeTests) + ".Property");

// Each method row borrows the same services from the temporary data-source instance. Disposing
// its constructor row with the first test invalidates the services used by every later test.
[EngineTest(ExpectedResult.Pass)]
[LentConstructorDependencyInjection]
[NotInParallel]
public class LentConstructorScopeTests(ProbeService constructorService)
{
    [TemporaryPropertyDependencyInjection]
    public required ProbeService PropertyService { get; init; }

    public IEnumerable<(ProbeService, ProbeService)> Rows()
    {
        for (var i = 0; i < 3; i++)
        {
            yield return (constructorService, PropertyService);
        }
    }

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public async Task Borrowed_Services_Survive_Every_Method_Row(ProbeService borrowedConstructor, ProbeService borrowedProperty)
    {
        await Assert.That(borrowedConstructor.Scope.DisposeCount).IsEqualTo(0);
        await Assert.That(borrowedConstructor.DisposeCount).IsEqualTo(0);
        await Assert.That(borrowedProperty.Scope.DisposeCount).IsEqualTo(0);
        await Assert.That(borrowedProperty.DisposeCount).IsEqualTo(0);
    }
}
