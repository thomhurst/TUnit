using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Building;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Collects, per test method of an open generic test class, the class type arguments that break the
/// class's generic constraints, and turns them into one failed test per violation (#7107).
/// </summary>
/// <remarks>
/// The source generator reports the same violations through <see cref="GenericConstraintViolationEntry"/>.
/// Both modes build the failed tests from <see cref="GenericConstraintViolationTestMetadata"/>, in the
/// same order (class-level <see cref="GenerateGenericTestAttribute"/> types, then class rows, then
/// method rows), so the names, messages and test ids match.
/// </remarks>
[RequiresUnreferencedCode("Uses reflection to analyze generic types")]
[RequiresDynamicCode("Uses reflection to analyze generic types")]
internal sealed class ReflectionGenericConstraintViolations(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)] Type genericTypeDefinition,
    MethodInfo[] testMethods)
{
    private Dictionary<MethodInfo, List<string>>? _messagesByMethod;

    /// <summary>
    /// Records <paramref name="typeArguments"/> as a violation for every test method when they break
    /// the class's constraints. Returns <c>true</c> when they do, so the caller skips the instantiation.
    /// </summary>
    public bool TryAddClassViolation(Type[] typeArguments)
    {
        var message = ReflectionGenericTypeResolver.FindConstraintViolation(genericTypeDefinition, typeArguments);
        if (message == null)
        {
            return false;
        }

        foreach (var testMethod in testMethods)
        {
            Add(testMethod, message);
        }

        return true;
    }

    /// <summary>
    /// For a non-generic test method of the generic class, records each method-level
    /// <see cref="ArgumentsAttribute"/> row whose values infer class type arguments that break the
    /// class's constraints, as the source generator does.
    /// </summary>
    /// <remarks>
    /// Only invalid rows are reported here. Running the valid rows of such methods is tracked by #7106.
    /// When <see cref="GenerateGenericTestAttribute"/> or a class-level data source already supplies the
    /// class type arguments (<paramref name="classTypeArgumentsSupplied"/>), method rows do not infer them, so
    /// nothing is reported. Class-level [Arguments] rows whose values go to no type parameter supply none (#7209).
    /// </remarks>
    public void AddMethodRowViolations(bool classTypeArgumentsSupplied)
    {
        if (classTypeArgumentsSupplied)
        {
            return;
        }

        var classGenericParameters = genericTypeDefinition.GetGenericArguments();

        foreach (var testMethod in testMethods)
        {
            if (testMethod.IsGenericMethodDefinition)
            {
                continue;
            }

            var parameters = testMethod.GetParameters();

            foreach (var argumentsAttribute in testMethod.GetCustomAttributes<ArgumentsAttribute>(inherit: false))
            {
                // [Arguments(null)] (a null params array) stores one null value, which infers object
                // here, as in the source generator (#7141)
                var typeArguments = InferClassTypeArguments(classGenericParameters, parameters, argumentsAttribute.Values);
                if (typeArguments == null)
                {
                    continue;
                }

                var message = ReflectionGenericTypeResolver.FindConstraintViolation(genericTypeDefinition, typeArguments);
                if (message != null)
                {
                    Add(testMethod, message);
                }
            }
        }
    }

    /// <summary>
    /// Creates one metadata per test method with violations. Each violation becomes one failed test.
    /// </summary>
    public IEnumerable<TestMetadata> CreateMetadata()
    {
        if (_messagesByMethod == null)
        {
            yield break;
        }

        foreach (var testMethod in testMethods)
        {
            if (!_messagesByMethod.TryGetValue(testMethod, out var messages))
            {
                continue;
            }

            var testAttribute = testMethod.GetCustomAttribute<TestAttribute>();

            yield return GenericConstraintViolationTestMetadata.Create(
                genericTypeDefinition,
                testMethod.Name,
                ReflectionMetadataBuilder.CreateMethodMetadata(genericTypeDefinition, testMethod),
                testAttribute?.File ?? "Unknown",
                testAttribute?.Line ?? 0,
                messages);
        }
    }

    private void Add(MethodInfo testMethod, string message)
    {
        _messagesByMethod ??= [];
        if (!_messagesByMethod.TryGetValue(testMethod, out var messages))
        {
            messages = [];
            _messagesByMethod[testMethod] = messages;
        }

        messages.Add(message);
    }

    private static Type[]? InferClassTypeArguments(Type[] classGenericParameters, ParameterInfo[] parameters, object?[] values)
    {
        var typeArguments = new Type?[classGenericParameters.Length];

        // Row values bind to the data parameters only; the engine supplies any CancellationToken, wherever it
        // is, as in InferClassTypeArgumentsFromMethodRow and the generator's GetDataParameters
        if (Array.Exists(parameters, static parameter => parameter.ParameterType == typeof(CancellationToken)))
        {
            parameters = Array.FindAll(parameters, static parameter => parameter.ParameterType != typeof(CancellationToken));
        }

        for (var i = 0; i < parameters.Length && i < values.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (!parameterType.IsGenericParameter || parameterType.DeclaringMethod != null)
            {
                continue;
            }

            // Match the source generator: Roslyn types a null element of the params object?[] argument
            // as object, and a Type value as System.Type rather than its RuntimeType instance.
            var position = parameterType.GenericParameterPosition;
            typeArguments[position] ??= values[i] switch
            {
                null => typeof(object),
                Type => typeof(Type),
                var value => value.GetType()
            };
        }

        foreach (var typeArgument in typeArguments)
        {
            if (typeArgument == null)
            {
                return null;
            }
        }

        return typeArguments!;
    }
}
