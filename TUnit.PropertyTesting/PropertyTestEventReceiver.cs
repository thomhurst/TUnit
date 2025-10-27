using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyTesting;
using TUnit.Core.Services;
using TUnit.PropertyTesting.Shrinkers;

namespace TUnit.PropertyTesting;

/// <summary>
/// Event receiver that detects test failures and triggers shrinking for property-based tests.
/// </summary>
public class PropertyTestEventReceiver : ITestEndEventReceiver
{
    /// <summary>
    /// Gets the execution order for this event receiver.
    /// Property test shrinking should happen after other test end processing.
    /// </summary>
    public int Order => 1000;
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:MakeGenericMethod",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    public async ValueTask OnTestEnd(TestContext context)
    {
        // Only process failures
        if (context.Result?.State != TestState.Failed)
        {
            return;
        }

        // Get property test metadata
        var metadata = context.GetPropertyTestMetadata();
        if (metadata == null)
        {
            return; // Not a property test
        }

        // Only shrink original tests, not shrink attempts
        if (metadata.IsShrinkingTest)
        {
            return;
        }

        // Check shrink limiter
        var shrinkLimiter = context.GetService<ShrinkLimiter>();
        if (shrinkLimiter == null || !shrinkLimiter.CanShrink(metadata.OriginalTestId))
        {
            return;
        }

        // Store the failing inputs for shrinking
        metadata.OriginalFailingInputs = context.TestDetails.TestMethodArguments;

        // Get the test class type
        var classType = context.TestDetails.ClassType;
        var methodName = context.TestDetails.MethodName;

        // Generate shrunk test cases
        await GenerateShrunkTests(context, metadata, classType, methodName, shrinkLimiter);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:MakeGenericMethod",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2071:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "Property-based testing uses dynamic code generation. AOT support deferred for MVP.")]
    private async Task GenerateShrunkTests(
        TestContext context,
        PropertyTestMetadata metadata,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.NonPublicFields
            | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type classType,
        string methodName,
        ShrinkLimiter shrinkLimiter)
    {
        var originalArgs = metadata.OriginalFailingInputs;
        if (originalArgs == null || originalArgs.Length == 0)
        {
            return;
        }

        // Get method info for creating test lambda
        var methodInfo = classType.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo == null)
        {
            return;
        }

        // Get shrunk parameter combinations
        var shrunkCombinations = GenerateShrunkCombinations(originalArgs, methodInfo);

        // Use reflection to call AddDynamicTest<T> with the correct generic type
        var addDynamicTestMethod = typeof(TestContextExtensions)
            .GetMethod(nameof(TestContextExtensions.AddDynamicTest),
                BindingFlags.Public | BindingFlags.Static);

        if (addDynamicTestMethod == null)
        {
            return;
        }

        var genericAddDynamicTest = addDynamicTestMethod.MakeGenericMethod(classType);

        // Create shrunk tests for each combination
        foreach (var shrunkArgs in shrunkCombinations)
        {
            // Check if we've hit the shrink limit
            if (!shrinkLimiter.CanShrink(metadata.OriginalTestId))
            {
                break;
            }

            var shrinkAttempt = shrinkLimiter.IncrementAndGet(metadata.OriginalTestId);

            // Create a new PropertyTestMetadata for the shrunk test
            var shrunkMetadata = new PropertyTestMetadata
            {
                OriginalTestId = metadata.OriginalTestId,
                ShrinkAttempt = shrinkAttempt,
                MaxShrinkAttempts = metadata.MaxShrinkAttempts,
                OriginalFailingInputs = shrunkArgs,
                RandomSeed = metadata.RandomSeed,
                IsShrinkingTest = true
            };

            // Create DynamicTest<T> using reflection
            var dynamicTestType = typeof(DynamicTest<>).MakeGenericType(classType);
            var dynamicTest = Activator.CreateInstance(dynamicTestType);

            if (dynamicTest == null)
            {
                continue;
            }

            // Set properties on the dynamic test
            var testMethodProp = dynamicTestType.GetProperty(nameof(DynamicTest<object>.TestMethod));
            var testMethodArgsProp = dynamicTestType.GetProperty(nameof(DynamicTest<object>.TestMethodArguments));
            var testClassArgsProp = dynamicTestType.GetProperty(nameof(DynamicTest<object>.TestClassArguments));
            var parentTestIdProp = dynamicTestType.GetProperty(nameof(DynamicTest<object>.ParentTestId));
            var objectBagProp = dynamicTestType.GetProperty(nameof(DynamicTest<object>.ObjectBag));

            // Create lambda expression: testClass => testClass.MethodName(args...)
            // For async methods, we need to block on the task result to make it compatible with Expression<Action<T>>
            var paramExpr = Expression.Parameter(classType, "testClass");
            var argExprs = shrunkArgs.Select((arg, i) =>
                Expression.Constant(arg, methodInfo.GetParameters()[i].ParameterType)).ToArray();
            var callExpr = Expression.Call(paramExpr, methodInfo, argExprs);

            Expression bodyExpr;
            if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                // For Task-returning methods, call GetAwaiter().GetResult() to make it synchronous
                var getAwaiterMethod = methodInfo.ReturnType.GetMethod("GetAwaiter", Type.EmptyTypes);
                var getResultMethod = getAwaiterMethod?.ReturnType.GetMethod("GetResult", Type.EmptyTypes);

                if (getAwaiterMethod != null && getResultMethod != null)
                {
                    var awaiterExpr = Expression.Call(callExpr, getAwaiterMethod);
                    bodyExpr = Expression.Call(awaiterExpr, getResultMethod);
                }
                else
                {
                    // Fallback: just call the method and ignore the Task
                    bodyExpr = callExpr;
                }
            }
            else
            {
                bodyExpr = callExpr;
            }

            // Create the typed lambda expression
            var actionType = typeof(Action<>).MakeGenericType(classType);
            var lambda = Expression.Lambda(actionType, bodyExpr, paramExpr);

            testMethodProp?.SetValue(dynamicTest, lambda);
            testMethodArgsProp?.SetValue(dynamicTest, shrunkArgs);
            testClassArgsProp?.SetValue(dynamicTest, context.TestDetails.TestClassArguments);
            parentTestIdProp?.SetValue(dynamicTest, context.Id);

            // Set up ObjectBag with metadata
            var objectBag = new Dictionary<string, object?>
            {
                ["PropertyTestMetadata"] = shrunkMetadata
            };
            objectBagProp?.SetValue(dynamicTest, objectBag);

            // Invoke AddDynamicTest<T>(context, dynamicTest)
            try
            {
                var task = genericAddDynamicTest.Invoke(null, [context, dynamicTest]) as Task;
                if (task != null)
                {
                    await task;
                }
            }
            catch
            {
                // Silently ignore errors during shrink test registration
                // to prevent cascading failures
            }
        }
    }

    private IEnumerable<object?[]> GenerateShrunkCombinations(object?[] originalArgs, MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();

        // For MVP, shrink each parameter independently
        for (var i = 0; i < originalArgs.Length; i++)
        {
            var arg = originalArgs[i];
            if (arg == null)
            {
                continue;
            }

            var parameter = i < parameters.Length ? parameters[i] : null;
            var shrunkValues = ShrinkValue(arg, parameter);
            foreach (var shrunkValue in shrunkValues)
            {
                var shrunkArgs = new object?[originalArgs.Length];
                Array.Copy(originalArgs, shrunkArgs, originalArgs.Length);
                shrunkArgs[i] = shrunkValue;
                yield return shrunkArgs;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate shrinkers. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate shrinkers. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate shrinkers. AOT support deferred for MVP.")]
    private IEnumerable<object?> ShrinkValue(object value, ParameterInfo? parameter)
    {
        // Check for custom shrinker in PropertyDataAttribute
        Type? customShrinkerType = null;
        if (parameter != null)
        {
            var propertyDataAttr = parameter.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().IsGenericType &&
                                   a.GetType().GetGenericTypeDefinition().Name == "PropertyDataAttribute`1");

            if (propertyDataAttr != null)
            {
                var shrinkerTypeProp = propertyDataAttr.GetType().GetProperty("ShrinkerType");
                customShrinkerType = shrinkerTypeProp?.GetValue(propertyDataAttr) as Type;
            }
        }

        // Use shrinker abstraction based on value type
        return value switch
        {
            int intValue => ShrinkWithShrinker(intValue, customShrinkerType),
            long longValue => ShrinkWithShrinker(longValue, customShrinkerType),
            double doubleValue => ShrinkWithShrinker(doubleValue, customShrinkerType),
            bool boolValue => ShrinkWithShrinker(boolValue, customShrinkerType),
            string stringValue => ShrinkWithShrinker(stringValue, customShrinkerType),
            _ => []
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate shrinkers. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate shrinkers. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate shrinkers. AOT support deferred for MVP.")]
    private IEnumerable<object?> ShrinkWithShrinker<T>(
        T value,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type? customShrinkerType)
    {
        IShrinker<T> shrinker;

        // If custom shrinker specified, instantiate it
        if (customShrinkerType != null)
        {
            var shrinkerInstance = Activator.CreateInstance(customShrinkerType);
            if (shrinkerInstance is IShrinker<T> typedShrinker)
            {
                shrinker = typedShrinker;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Custom shrinker type '{customShrinkerType.Name}' does not implement IShrinker<{typeof(T).Name}>");
            }
        }
        else
        {
            // Use built-in shrinkers
            shrinker = GetDefaultShrinker<T>();
        }

        return shrinker.Shrink(value).Cast<object?>();
    }

    private IShrinker<T> GetDefaultShrinker<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (IShrinker<T>)(object)new IntShrinker();
        }

        if (typeof(T) == typeof(long))
        {
            return (IShrinker<T>)(object)new LongShrinker();
        }

        if (typeof(T) == typeof(double))
        {
            return (IShrinker<T>)(object)new DoubleShrinker();
        }

        if (typeof(T) == typeof(bool))
        {
            return (IShrinker<T>)(object)new BoolShrinker();
        }

        if (typeof(T) == typeof(string))
        {
            return (IShrinker<T>)(object)new StringShrinker();
        }

        throw new NotSupportedException(
            $"No built-in shrinker for type '{typeof(T).Name}'. " +
            $"Specify a custom shrinker using PropertyDataAttribute.ShrinkerType.");
    }
}
