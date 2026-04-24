namespace TUnit.Core;

/// <summary>
/// Unified executable test implementation that works for both AOT and reflection modes.
/// Replaces ExecutableTest<T> and DynamicExecutableTest with a single implementation.
/// All mode-specific logic is handled during delegate creation, not execution.
/// </summary>
public class ExecutableTest : AbstractExecutableTest
{
    // Null only inside typed subclasses that override CreateInstanceAsync/InvokeTestAsync and
    // therefore never invoke the delegates. The parameterless constructor enforces that invariant.
    private readonly Func<TestContext, Task<object>>? _createInstance;
    private readonly Func<object, object?[], TestContext, CancellationToken, Task>? _invokeTest;

    /// <summary>
    /// Constructor for subclasses that supply their own invocation logic by overriding
    /// <see cref="CreateInstanceAsync"/> and <see cref="InvokeTestAsync"/> directly.
    /// </summary>
    private protected ExecutableTest()
    {
    }

    /// <summary>
    /// Creates an ExecutableTest where mode-specific behavior is encapsulated in the delegates.
    /// Both AOT and reflection modes provide delegates with identical signatures.
    /// </summary>
    /// <param name="createInstance">Delegate that creates the test instance with all necessary initialization</param>
    /// <param name="invokeTest">Delegate that invokes the test method with proper parameter handling</param>
    public ExecutableTest(
        Func<TestContext, Task<object>> createInstance,
        Func<object, object?[], TestContext, CancellationToken, Task> invokeTest)
    {
        _createInstance = createInstance ?? throw new ArgumentNullException(nameof(createInstance));
        _invokeTest = invokeTest ?? throw new ArgumentNullException(nameof(invokeTest));
    }

    public override async Task<object> CreateInstanceAsync()
    {
        return await _createInstance!(Context);
    }

    public override async Task InvokeTestAsync(object instance, CancellationToken cancellationToken)
    {
        await _invokeTest!(instance, Arguments, Context, cancellationToken);
    }
}
