using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Strongly typed executable test that stores the per-test delegates from
/// <see cref="TestMetadata{T}"/> as fields instead of capturing them in closures. This avoids the
/// two closure allocations (<c>createInstance</c> and <c>invokeTest</c>) that the generic factory
/// would otherwise allocate per test invocation — on a 1000-test run that is 2000 delegates saved.
/// Inherits directly from <see cref="AbstractExecutableTest"/> so <see cref="ExecutableTest"/>
/// can remain sealed (public API surface).
/// </summary>
/// <typeparam name="T">The test class type.</typeparam>
internal sealed class ExecutableTest<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    T> : AbstractExecutableTest where T : class
{
    // Fields that the previous closures captured. ClassArguments and Arguments live on the
    // base AbstractExecutableTest, so only the metadata reference plus the two ExecutableTestCreationContext
    // values that aren't mirrored on the base need to be stored here.
    private readonly TestMetadata<T> _metadata;
    private readonly Func<Task<object>>? _testClassInstanceFactory;
    private readonly Type[] _resolvedClassGenericArguments;

    public ExecutableTest(
        TestMetadata<T> metadata,
        ExecutableTestCreationContext context)
    {
        _metadata = metadata;
        _testClassInstanceFactory = context.TestClassInstanceFactory;
        _resolvedClassGenericArguments = context.ResolvedClassGenericArguments;
    }

    public override async Task<object> CreateInstanceAsync()
    {
        if (_testClassInstanceFactory != null)
        {
            return await _testClassInstanceFactory();
        }

        var attributes = _metadata.GetOrCreateAttributes();
        var instance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
            attributes,
            _metadata.TestClassType,
            _metadata.TestSessionId,
            Context);

        if (instance != null)
        {
            return instance;
        }

        return _metadata.InstanceFactory!(_resolvedClassGenericArguments, ClassArguments);
    }

    public override async Task InvokeTestAsync(object instance, CancellationToken cancellationToken)
    {
        await _metadata.InvokeTypedTest!((T)instance, Arguments, cancellationToken).ConfigureAwait(false);
    }
}
