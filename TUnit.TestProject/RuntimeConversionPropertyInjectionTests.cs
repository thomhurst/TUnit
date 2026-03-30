#pragma warning disable TUnit0042

using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

#region Types for runtime conversion tests

// --- Implicit operator on source type (source defines how to convert to target) ---

public class RuntimeSourceWithImplicit
{
    public string Value { get; init; } = "from-source-implicit";
    public static implicit operator RuntimeTarget(RuntimeSourceWithImplicit s) => new() { Value = s.Value };
}

public class RuntimeTarget
{
    public string Value { get; init; } = "";
}

// --- Explicit operator on source type ---

public class RuntimeSourceWithExplicit
{
    public string Value { get; init; } = "from-source-explicit";
    public static explicit operator RuntimeExplicitTarget(RuntimeSourceWithExplicit s) => new() { Value = s.Value };
}

public class RuntimeExplicitTarget
{
    public string Value { get; init; } = "";
}

// --- Implicit operator on target type (target defines how to convert from source) ---

public class RuntimeTargetDefinesImplicit
{
    public string Value { get; init; } = "";
    public static implicit operator RuntimeTargetDefinesImplicit(RuntimeSourceForTargetImplicit s) => new() { Value = s.Value };
}

public class RuntimeSourceForTargetImplicit
{
    public string Value { get; init; } = "from-target-implicit";
}

// --- Struct with implicit operator ---

public readonly struct RuntimeValueWrapper
{
    public string Value { get; init; }
    public RuntimeValueWrapper(string value) => Value = value;
    public static implicit operator RuntimeValueTarget(RuntimeValueWrapper w) => new() { Value = w.Value };
}

public class RuntimeValueTarget
{
    public string Value { get; init; } = "";
}

// --- Interface-based conversion (same type, no conversion needed) ---

public class RuntimeSameTypeData : IAsyncDisposable
{
    public string Value { get; init; } = "same-type";
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// --- Custom UntypedDataSourceGeneratorAttribute implementations ---

/// <summary>
/// Custom data source that yields a RuntimeSourceWithImplicit instance
/// for a property typed as RuntimeTarget. The source generator has no type info
/// for the produced value — conversion must happen at runtime via CastHelper.
/// </summary>
public class ImplicitSourceDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [new RuntimeSourceWithImplicit { Value = "custom-implicit" }];
    }
}

/// <summary>
/// Custom data source that yields a RuntimeSourceWithExplicit instance
/// for a property typed as RuntimeExplicitTarget.
/// </summary>
public class ExplicitSourceDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [new RuntimeSourceWithExplicit { Value = "custom-explicit" }];
    }
}

/// <summary>
/// Custom data source where the target type defines the implicit conversion from the source type.
/// </summary>
public class TargetDefinesImplicitDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [new RuntimeSourceForTargetImplicit { Value = "target-defines-implicit" }];
    }
}

/// <summary>
/// Custom data source that yields a value type (struct) with an implicit operator.
/// </summary>
public class StructImplicitDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [new RuntimeValueWrapper("struct-implicit")];
    }
}

/// <summary>
/// Custom data source that yields the same type as the property (no conversion needed).
/// Baseline test to ensure no regression when types match.
/// </summary>
public class SameTypeDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [new RuntimeSameTypeData { Value = "same-type-custom" }];
    }
}

#endregion

#region Test classes

/// <summary>
/// Tests the runtime conversion fallback with a custom untyped data source
/// that yields a type with an implicit operator defined on the source type.
/// The source generator cannot know what type the custom data source will produce,
/// so conversion must happen at runtime via CastHelper.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RuntimeImplicitConversionFromCustomDataSourceTests
{
    [ImplicitSourceDataSource]
    public required RuntimeTarget Target { get; init; }

    [Test]
    public async Task Custom_DataSource_With_Implicit_Operator_On_Source()
    {
        await Assert.That(Target).IsNotNull();
        await Assert.That(Target.Value).IsEqualTo("custom-implicit");
    }
}

/// <summary>
/// Tests the runtime conversion fallback with a custom untyped data source
/// that yields a type with an explicit operator.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RuntimeExplicitConversionFromCustomDataSourceTests
{
    [ExplicitSourceDataSource]
    public required RuntimeExplicitTarget Target { get; init; }

    [Test]
    public async Task Custom_DataSource_With_Explicit_Operator()
    {
        await Assert.That(Target).IsNotNull();
        await Assert.That(Target.Value).IsEqualTo("custom-explicit");
    }
}

/// <summary>
/// Tests the runtime conversion fallback where the target type defines the implicit
/// conversion operator (as opposed to the source type).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RuntimeImplicitOnTargetTypeConversionTests
{
    [TargetDefinesImplicitDataSource]
    public required RuntimeTargetDefinesImplicit Target { get; init; }

    [Test]
    public async Task Custom_DataSource_With_Implicit_Operator_On_Target()
    {
        await Assert.That(Target).IsNotNull();
        await Assert.That(Target.Value).IsEqualTo("target-defines-implicit");
    }
}

/// <summary>
/// Tests the runtime conversion fallback with a struct (value type) that defines
/// an implicit operator. This exercises the boxing/unboxing + conversion path.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RuntimeStructImplicitConversionTests
{
    [StructImplicitDataSource]
    public required RuntimeValueTarget Target { get; init; }

    [Test]
    public async Task Custom_DataSource_With_Struct_Implicit_Operator()
    {
        await Assert.That(Target).IsNotNull();
        await Assert.That(Target.Value).IsEqualTo("struct-implicit");
    }
}

/// <summary>
/// Baseline test: custom data source yields the same type as the property.
/// No conversion is needed — verifies no regression when types already match.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RuntimeSameTypeNoConversionTests
{
    [SameTypeDataSource]
    public required RuntimeSameTypeData Data { get; init; }

    [Test]
    public async Task Custom_DataSource_Same_Type_No_Conversion_Needed()
    {
        await Assert.That(Data).IsNotNull();
        await Assert.That(Data.Value).IsEqualTo("same-type-custom");
    }
}

/// <summary>
/// Tests the runtime conversion fallback via MethodDataSource which returns a
/// different type than the property type. The method returns RuntimeSourceWithImplicit
/// but the property expects RuntimeTarget.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class RuntimeMethodDataSourceImplicitConversionTests
{
    [MethodDataSource(nameof(GetSource))]
    public required RuntimeTarget Target { get; init; }

    public static RuntimeSourceWithImplicit GetSource() => new() { Value = "method-source-implicit" };

    [Test]
    public async Task MethodDataSource_With_Implicit_Operator_Runtime_Conversion()
    {
        await Assert.That(Target).IsNotNull();
        await Assert.That(Target.Value).IsEqualTo("method-source-implicit");
    }
}

#endregion
