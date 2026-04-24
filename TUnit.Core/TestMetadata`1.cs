using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Strongly typed test metadata that provides type-safe test invocation
/// </summary>
/// <typeparam name="T">The type of the test class</typeparam>
public class TestMetadata<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    T> : TestMetadata, ITypedTestMetadata where T : class
{
    private Func<Type[], object?[], T>? _instanceFactory;
    private Func<T, object?[], Task>? _testInvoker;
    private Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest>? _cachedExecutableTestFactory;

    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<Type[], object?[], T>? InstanceFactory
    {
        get => _instanceFactory;
        init
        {
            _instanceFactory = value;
            if (value != null)
            {
                base.InstanceFactory = (typeArgs, args) => value(typeArgs, args);
            }
        }
    }

    /// <summary>
    /// Strongly typed test invoker
    /// </summary>
    public new Func<T, object?[], Task>? TestInvoker
    {
        get => _testInvoker;
        init
        {
            _testInvoker = value;
            if (value != null)
            {
                base.TestInvoker = (instance, args) => value((T)instance, args);
            }
        }
    }


    /// <summary>
    /// Strongly typed test invoker with CancellationToken support.
    /// Used by source generation mode (per-method delegates, generic/inherited tests).
    /// </summary>
    public Func<T, object?[], CancellationToken, ValueTask>? InvokeTypedTest { get; init; }

    /// <summary>
    /// Class-shared indexed invoker emitted by the source generator. All tests in a class share this
    /// delegate and dispatch via <see cref="MethodIndex"/>, so the TestEntry → TestMetadata bridge can
    /// forward the static delegate without allocating a per-test closure capturing <c>this</c>.
    /// </summary>
    internal Func<T, int, object?[], CancellationToken, ValueTask>? IndexedInvokeBody { get; init; }

    /// <summary>Index passed to <see cref="IndexedInvokeBody"/> when dispatching.</summary>
    internal int MethodIndex { get; init; }

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// Uses a static factory method so the cached delegate is a pure method reference —
    /// no closure, and per-test state is stored on the returned <see cref="ExecutableTest{T}"/>
    /// instead of in captured lambdas.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            // Volatile-equivalent read via Interlocked pattern mirrors TestEntry's cached-delegate
            // idiom: first reader computes, subsequent racers lose the CompareExchange and fall
            // through to re-read the now-published field. Keeps this allocation-free on the fast
            // path while guaranteeing publication on weakly-ordered architectures.
            var cached = _cachedExecutableTestFactory;
            if (cached != null)
            {
                return cached;
            }

            if (InstanceFactory != null && (InvokeTypedTest != null || IndexedInvokeBody != null))
            {
                Interlocked.CompareExchange<Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest>?>(
                    ref _cachedExecutableTestFactory, CreateTypedExecutableTest, null);
                return _cachedExecutableTestFactory!;
            }

            // Delegating the throw to a helper keeps this getter itself throw-free (Sonar S2372) —
            // the abstract base declares this as a property so it must stay a property, and the
            // misconfiguration is a programmer error rather than a recoverable condition.
            return ThrowMissingInvoker();
        }
    }

    [DoesNotReturn]
    private static Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> ThrowMissingInvoker() =>
        throw new InvalidOperationException(
            $"InstanceFactory and an invoker (InvokeTypedTest or IndexedInvokeBody) must be set for {typeof(T).Name}");

    private static AbstractExecutableTest CreateTypedExecutableTest(
        ExecutableTestCreationContext context,
        TestMetadata metadata)
    {
        var typedMetadata = (TestMetadata<T>)metadata;
        return new ExecutableTest<T>(typedMetadata, context)
        {
            TestId = context.TestId,
            Metadata = metadata,
            Arguments = context.Arguments,
            ClassArguments = context.ClassArguments,
            Context = context.Context
        };
    }

    /// <summary>
    /// Sets up runtime data combination generation for this test by updating the TestSessionId
    /// </summary>
    public void UseRuntimeDataGeneration(string testSessionId)
    {
        TestSessionId = testSessionId;
    }
}
