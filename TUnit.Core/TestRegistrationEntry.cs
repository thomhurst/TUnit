using System.ComponentModel;

namespace TUnit.Core;

/// <summary>
/// Lightweight, pure-data representation of a test for registration and filtering.
/// Contains no delegates or method references — only static data that can be
/// loaded from the PE image without JIT compilation.
/// </summary>
/// <remarks>
/// <para>
/// This struct enables O(1) JIT startup for source-generated test suites by
/// separating test metadata (data) from test invocation (behavior). The engine
/// can filter tests by iterating over these entries without triggering JIT
/// compilation of any per-test methods.
/// </para>
/// <para>
/// Used by the source generator's data-table registration path. Generic and
/// inherited tests continue to use <see cref="Interfaces.SourceGenerator.ITestSource"/>.
/// </para>
/// </remarks>
#if !DEBUG
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public readonly struct TestRegistrationEntry
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// Format: "{Namespace}.{ClassName}.{MethodName}:{DataIndex}"
    /// </summary>
    public required string TestId { get; init; }

    /// <summary>
    /// Gets the simple name of the test class.
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Gets the name of the test method.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets the fully qualified name of the test.
    /// Format: "{Namespace}.{ClassName}.{MethodName}"
    /// </summary>
    public required string FullyQualifiedName { get; init; }

    /// <summary>
    /// Gets the source file path where the test is defined.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the line number where the test is defined.
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    /// Gets the pre-extracted category values from CategoryAttribute.
    /// </summary>
    public string[] Categories { get; init; }

    /// <summary>
    /// Gets the pre-extracted property values from PropertyAttribute.
    /// Format: "key=value" pairs.
    /// </summary>
    public string[] Properties { get; init; }

    /// <summary>
    /// Gets the dependency strings from DependsOnAttribute.
    /// Format: "ClassName:MethodName" for cross-class, ":MethodName" for same-class,
    /// "ClassName:" for all tests in class.
    /// </summary>
    public string[] DependsOn { get; init; }

    /// <summary>
    /// Gets whether this test has data sources (is parameterized).
    /// </summary>
    public required bool HasDataSource { get; init; }

    /// <summary>
    /// Gets the repeat count from RepeatAttribute, or 0 if not present.
    /// </summary>
    public required int RepeatCount { get; init; }

    /// <summary>
    /// Gets the index into the assembly's class type array.
    /// Used to resolve the test class Type without storing a Type reference per entry.
    /// </summary>
    public required int ClassTypeIndex { get; init; }

    /// <summary>
    /// Gets the index of this method within its class's method table.
    /// Used for switch-based dispatch in the per-class materializer.
    /// </summary>
    public required int MethodIndex { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestRegistrationEntry"/> struct.
    /// </summary>
    public TestRegistrationEntry()
    {
        Categories = [];
        Properties = [];
        DependsOn = [];
    }
}
