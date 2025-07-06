namespace TUnit.Engine;

/// <summary>
/// Dynamic executable test that works with runtime type information
/// </summary>
internal sealed class DynamicExecutableTest : ExecutableTest
{
    private readonly Func<Task<object>> _createInstance;
    private readonly Func<object, object?[], Task> _invokeTest;

    public DynamicExecutableTest(
        Func<Task<object>> createInstance,
        Func<object, object?[], Task> invokeTest)
    {
        _createInstance = createInstance;
        _invokeTest = invokeTest;
    }

    public override Task<object> CreateInstanceAsync()
    {
        return _createInstance();
    }

    public override async Task InvokeTestAsync(object instance, CancellationToken cancellationToken)
    {
        // Check if any of the test method parameters expect a CancellationToken
        var parameterTypes = Metadata.ParameterTypes;
        var hasCancellationToken = false;
        var cancellationTokenIndex = -1;

        for (var i = 0; i < parameterTypes.Length; i++)
        {
            if (parameterTypes[i] == typeof(CancellationToken))
            {
                hasCancellationToken = true;
                cancellationTokenIndex = i;
                break;
            }
        }

        if (hasCancellationToken)
        {
            // Create a new arguments array with the CancellationToken inserted
            var argsWithToken = new object?[Arguments.Length + 1];

            // Copy arguments before the CancellationToken position
            for (var i = 0; i < cancellationTokenIndex && i < Arguments.Length; i++)
            {
                argsWithToken[i] = Arguments[i];
            }

            // Insert the CancellationToken
            argsWithToken[cancellationTokenIndex] = cancellationToken;

            // Copy remaining arguments
            for (var i = cancellationTokenIndex; i < Arguments.Length; i++)
            {
                argsWithToken[i + 1] = Arguments[i];
            }

            await _invokeTest(instance, argsWithToken);
        }
        else
        {
            // No CancellationToken expected, use arguments as-is
            await _invokeTest(instance, Arguments);
        }
    }
}
