using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3939;

/// <summary>
/// Regression test for issue #3939: IClassConstructor and ITestEndEventReceiver
/// should use the same instance so that state can be shared (e.g., DI scope disposal).
/// </summary>
public sealed class ScopedClassConstructor : IClassConstructor, ITestEndEventReceiver
{
    private object? _scope;

    public Task<object> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        // Simulate creating a DI scope - this should be available in OnTestEnd
        _scope = new object();
        return Task.FromResult<object>(new Tests());
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        // This should be called on the SAME instance that Create() was called on
        // If _scope is null, it means a different instance was used - this is the bug!
        if (_scope == null)
        {
            throw new InvalidOperationException(
                "Issue #3939: _scope was null in OnTestEnd(), indicating a different IClassConstructor " +
                "instance was used than the one where Create() was called. " +
                "IClassConstructor and ITestEndEventReceiver must use the same instance.");
        }

        // Simulate disposing the scope
        _scope = null;
        return default;
    }

    public int Order => 0;
}

[EngineTest(ExpectedResult.Pass)]
[ClassConstructor<ScopedClassConstructor>]
public sealed class Tests
{
    [Test]
    public Task TestMethod()
    {
        // The actual test - just verifies the test runs
        // The real assertion is in OnTestEnd - it will throw if different instances are used
        return Task.CompletedTask;
    }
}
