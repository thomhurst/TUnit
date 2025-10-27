using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyTesting;

namespace TUnit.PropertyTesting;

/// <summary>
/// Generates random test data for property-based testing.
/// Use with <see cref="PropertyDataAttribute{T}"/> on parameters to specify generation rules.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PropertyDataSourceAttribute : UntypedDataSourceGeneratorAttribute, ITestStartEventReceiver, ITestEndEventReceiver
{
    /// <summary>
    /// Gets or sets the number of test cases to generate.
    /// Default is 100.
    /// </summary>
    public int TestCaseCount { get; init; } = 100;

    /// <summary>
    /// Gets or sets the random seed for reproducible test generation.
    /// If set to -1 (default), a random seed will be used.
    /// </summary>
    public long Seed { get; init; } = -1;

    /// <summary>
    /// Gets or sets the maximum number of shrink attempts when a test fails.
    /// Default is 1000.
    /// </summary>
    public int MaxShrinkAttempts { get; init; } = 1000;

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var parameterInformation = dataGeneratorMetadata
            .MembersToGenerate
            .OfType<ParameterMetadata>()
            .ToArray();

        if (parameterInformation.Length != dataGeneratorMetadata.MembersToGenerate.Length
            || parameterInformation.Length is 0)
        {
            throw new Exception("[PropertyDataSource] only supports parameterised tests");
        }

        if (dataGeneratorMetadata.TestInformation == null)
        {
            throw new InvalidOperationException("PropertyDataSource requires test information but none is available.");
        }

        // Use provided seed or generate a new one
        var seed = Seed == -1 ? DateTime.UtcNow.Ticks : Seed;
        var random = new Random((int)(seed % int.MaxValue));

        // Generate test cases
        for (var i = 0; i < TestCaseCount; i++)
        {
            var testCaseData = GenerateTestCase(parameterInformation, random, seed, dataGeneratorMetadata);
            yield return () => testCaseData;
        }
    }

    private object?[] GenerateTestCase(
        ParameterMetadata[] parameters,
        Random random,
        long seed,
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            result[i] = GenerateValueForParameter(parameter, random, dataGeneratorMetadata);
        }

        return result;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate generators. AOT support deferred for MVP.")]
    private object? GenerateValueForParameter(
        ParameterMetadata parameter,
        Random random,
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (parameter.ReflectionInfo == null)
        {
            throw new InvalidOperationException(
                $"Parameter reflection information is not available for parameter '{parameter.Name}'.");
        }

        // Check for PropertyDataAttribute
        var propertyDataAttr = parameter.ReflectionInfo
            .GetCustomAttributesSafe()
            .FirstOrDefault(a => a.GetType().IsGenericType &&
                               a.GetType().GetGenericTypeDefinition().Name == "PropertyDataAttribute`1");

        var parameterType = parameter.Type;

        // Get underlying type for nullable value types
        var underlyingType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

        // Check for custom generator type
        var generatorType = GetPropertyValue<Type?>(propertyDataAttr, "GeneratorType");

        // Use generator abstraction
        if (underlyingType == typeof(int))
        {
            var generator = CreateGenerator<int>(generatorType, propertyDataAttr);
            return generator.Generate(random);
        }

        if (underlyingType == typeof(bool))
        {
            var generator = CreateGenerator<bool>(generatorType, propertyDataAttr);
            return generator.Generate(random);
        }

        if (underlyingType == typeof(string))
        {
            var generator = CreateGenerator<string>(generatorType, propertyDataAttr);
            return generator.Generate(random);
        }

        if (underlyingType == typeof(long))
        {
            var generator = CreateGenerator<long>(generatorType, propertyDataAttr);
            return generator.Generate(random);
        }

        if (underlyingType == typeof(double))
        {
            var generator = CreateGenerator<double>(generatorType, propertyDataAttr);
            return generator.Generate(random);
        }

        throw new NotSupportedException(
            $"Property-based testing does not yet support parameter type '{parameterType.Name}'. " +
            $"Supported types: int, long, double, bool, string");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate generators. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate generators. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to instantiate generators. AOT support deferred for MVP.")]
    private IGenerator<T> CreateGenerator<T>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type? customGeneratorType,
        object? propertyDataAttr)
    {
        // If custom generator specified, instantiate it
        if (customGeneratorType != null)
        {
            var generator = Activator.CreateInstance(customGeneratorType);
            if (generator is IGenerator<T> typedGenerator)
            {
                return typedGenerator;
            }
            throw new InvalidOperationException(
                $"Custom generator type '{customGeneratorType.Name}' does not implement IGenerator<{typeof(T).Name}>");
        }

        // Use built-in generators based on type
        if (typeof(T) == typeof(int))
        {
            var min = GetPropertyValue<object?>(propertyDataAttr, "Min");
            var max = GetPropertyValue<object?>(propertyDataAttr, "Max");
            var minInt = min != null ? Convert.ToInt32(min) : int.MinValue / 2;
            var maxInt = max != null ? Convert.ToInt32(max) : int.MaxValue / 2;
            return (IGenerator<T>)(object)new Generators.IntGenerator(minInt, maxInt);
        }

        if (typeof(T) == typeof(long))
        {
            var min = GetPropertyValue<object?>(propertyDataAttr, "Min");
            var max = GetPropertyValue<object?>(propertyDataAttr, "Max");
            var minLong = min != null ? Convert.ToInt64(min) : long.MinValue / 2;
            var maxLong = max != null ? Convert.ToInt64(max) : long.MaxValue / 2;
            return (IGenerator<T>)(object)new Generators.LongGenerator(minLong, maxLong);
        }

        if (typeof(T) == typeof(double))
        {
            var min = GetPropertyValue<object?>(propertyDataAttr, "Min");
            var max = GetPropertyValue<object?>(propertyDataAttr, "Max");
            var minDouble = min != null ? Convert.ToDouble(min) : -1000000.0;
            var maxDouble = max != null ? Convert.ToDouble(max) : 1000000.0;
            return (IGenerator<T>)(object)new Generators.DoubleGenerator(minDouble, maxDouble);
        }

        if (typeof(T) == typeof(string))
        {
            var minLength = GetPropertyValue<int>(propertyDataAttr, "MinLength");
            var maxLength = GetPropertyValue<int>(propertyDataAttr, "MaxLength");
            minLength = minLength == -1 ? 0 : minLength;
            maxLength = maxLength == -1 ? 20 : maxLength;
            return (IGenerator<T>)(object)new Generators.StringGenerator(minLength, maxLength);
        }

        if (typeof(T) == typeof(bool))
        {
            return (IGenerator<T>)(object)new Generators.BoolGenerator();
        }

        throw new NotSupportedException(
            $"No built-in generator for type '{typeof(T).Name}'. " +
            $"Specify a custom generator using PropertyDataAttribute.GeneratorType.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to read attribute properties. AOT support deferred for MVP.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:UnrecognizedReflectionPattern",
        Justification = "Property-based testing uses reflection to read attribute properties. AOT support deferred for MVP.")]
    private T? GetPropertyValue<T>(object? attribute, string propertyName)
    {
        if (attribute == null) return default;

        var property = attribute.GetType().GetProperty(propertyName);
        if (property == null) return default;

        var value = property.GetValue(attribute);
        return value is T typedValue ? typedValue : default;
    }

    // ITestStartEventReceiver implementation
    /// <summary>
    /// Gets the execution order for this event receiver.
    /// Property test shrinking should happen after other test end processing.
    /// </summary>
    public int Order => 1000;

    /// <summary>
    /// Called when a test starts. Sets up PropertyTestMetadata for original tests.
    /// </summary>
    public ValueTask OnTestStart(TestContext context)
    {
        // Check if metadata already exists (for shrunk tests)
        var existingMetadata = context.GetPropertyTestMetadata();
        if (existingMetadata != null)
        {
            return default;
        }

        // Create metadata for original property test
        var metadata = new PropertyTestMetadata
        {
            OriginalTestId = context.Id,
            MaxShrinkAttempts = MaxShrinkAttempts,
            RandomSeed = Seed == -1 ? DateTime.UtcNow.Ticks : Seed,
            IsShrinkingTest = false,
            OriginalFailingInputs = null,
            ShrinkAttempt = 0
        };

        // Store in ObjectBag
        context.ObjectBag["PropertyTestMetadata"] = metadata;

        return default;
    }

    /// <summary>
    /// Called when a test ends. Triggers shrinking if the test failed.
    /// </summary>
    public async ValueTask OnTestEnd(TestContext context)
    {
        // Delegate to PropertyTestEventReceiver for shrinking logic
        var receiver = new PropertyTestEventReceiver();
        await receiver.OnTestEnd(context);
    }
}
