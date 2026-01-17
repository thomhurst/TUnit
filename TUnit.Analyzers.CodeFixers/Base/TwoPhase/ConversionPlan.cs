using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Base.TwoPhase;

/// <summary>
/// Represents the complete conversion plan for a single file.
/// Built during Phase 1 (Analysis) while the semantic model is valid.
/// Applied during Phase 2 (Transformation) using pure syntax operations.
/// </summary>
public class ConversionPlan
{
    /// <summary>
    /// Assertions to convert (e.g., Assert.Equal → Assert.That().IsEqualTo())
    /// </summary>
    public List<AssertionConversion> Assertions { get; } = new();

    /// <summary>
    /// Attributes to convert (e.g., [Fact] → [Test])
    /// </summary>
    public List<AttributeConversion> Attributes { get; } = new();

    /// <summary>
    /// Attributes to remove entirely (e.g., [TestClass])
    /// </summary>
    public List<AttributeRemoval> AttributeRemovals { get; } = new();

    /// <summary>
    /// Base types to remove (e.g., IClassFixture&lt;T&gt;)
    /// </summary>
    public List<BaseTypeRemoval> BaseTypeRemovals { get; } = new();

    /// <summary>
    /// Base types to add (e.g., IAsyncInitializer from IAsyncLifetime)
    /// </summary>
    public List<BaseTypeAddition> BaseTypeAdditions { get; } = new();

    /// <summary>
    /// Class attributes to add (e.g., ClassDataSource from IClassFixture)
    /// </summary>
    public List<ClassAttributeAddition> ClassAttributeAdditions { get; } = new();

    /// <summary>
    /// Method attributes to add (e.g., [Before(Test)] on InitializeAsync)
    /// </summary>
    public List<MethodAttributeAddition> MethodAttributeAdditions { get; } = new();

    /// <summary>
    /// Methods that need async/Task added to their signature
    /// </summary>
    public List<MethodSignatureChange> MethodSignatureChanges { get; } = new();

    /// <summary>
    /// Members to remove entirely (e.g., ITestOutputHelper fields)
    /// </summary>
    public List<MemberRemoval> MemberRemovals { get; } = new();

    /// <summary>
    /// Constructor parameters to remove (e.g., ITestOutputHelper)
    /// </summary>
    public List<ConstructorParameterRemoval> ConstructorParameterRemovals { get; } = new();

    /// <summary>
    /// Record.Exception conversions to try-catch blocks
    /// </summary>
    public List<RecordExceptionConversion> RecordExceptionConversions { get; } = new();

    /// <summary>
    /// Invocations to replace (e.g., _testOutputHelper.WriteLine → Console.WriteLine)
    /// </summary>
    public List<InvocationReplacement> InvocationReplacements { get; } = new();

    /// <summary>
    /// TheoryData conversions (TheoryData&lt;T&gt; → IEnumerable&lt;T&gt;)
    /// </summary>
    public List<TheoryDataConversion> TheoryDataConversions { get; } = new();

    /// <summary>
    /// Parameter attributes to convert (e.g., [Range(1, 5)] → [MatrixRange&lt;int&gt;(1, 5)])
    /// </summary>
    public List<ParameterAttributeConversion> ParameterAttributes { get; } = new();

    /// <summary>
    /// Usings to add
    /// </summary>
    public List<string> UsingsToAdd { get; } = new();

    /// <summary>
    /// Using prefixes to remove (e.g., "Xunit")
    /// </summary>
    public List<string> UsingPrefixesToRemove { get; } = new();

    /// <summary>
    /// Failures encountered during analysis
    /// </summary>
    public List<ConversionFailure> Failures { get; } = new();

    /// <summary>
    /// Returns true if the plan has any conversions to apply
    /// </summary>
    public bool HasConversions =>
        Assertions.Count > 0 ||
        Attributes.Count > 0 ||
        AttributeRemovals.Count > 0 ||
        BaseTypeRemovals.Count > 0 ||
        BaseTypeAdditions.Count > 0 ||
        ClassAttributeAdditions.Count > 0 ||
        MethodAttributeAdditions.Count > 0 ||
        MethodSignatureChanges.Count > 0 ||
        MemberRemovals.Count > 0 ||
        ConstructorParameterRemovals.Count > 0 ||
        RecordExceptionConversions.Count > 0 ||
        InvocationReplacements.Count > 0 ||
        TheoryDataConversions.Count > 0 ||
        ParameterAttributes.Count > 0 ||
        UsingsToAdd.Count > 0 ||
        UsingPrefixesToRemove.Count > 0;

    /// <summary>
    /// Returns true if any failures were recorded during analysis
    /// </summary>
    public bool HasFailures => Failures.Count > 0;
}

/// <summary>
/// Base class for all conversion targets. Uses SyntaxAnnotation to track nodes
/// across syntax tree modifications.
/// </summary>
public abstract class ConversionTarget
{
    /// <summary>
    /// Unique annotation to find this node after tree modifications
    /// </summary>
    public SyntaxAnnotation Annotation { get; } = new SyntaxAnnotation("TUnitMigration", Guid.NewGuid().ToString());

    /// <summary>
    /// Original source text for debugging/error messages
    /// </summary>
    public string OriginalText { get; init; } = "";
}

/// <summary>
/// Represents an assertion to convert
/// </summary>
public class AssertionConversion : ConversionTarget
{
    /// <summary>
    /// The type of assertion conversion to perform
    /// </summary>
    public required AssertionConversionKind Kind { get; init; }

    /// <summary>
    /// The new assertion code to generate (fully formed)
    /// </summary>
    public required string ReplacementCode { get; init; }

    /// <summary>
    /// Whether this assertion introduces an await expression
    /// </summary>
    public bool IntroducesAwait { get; init; }

    /// <summary>
    /// Optional TODO comment to add before the assertion
    /// </summary>
    public string? TodoComment { get; init; }
}

/// <summary>
/// Types of assertion conversions
/// </summary>
public enum AssertionConversionKind
{
    // Equality (xUnit naming)
    Equal,
    NotEqual,
    Same,
    NotSame,
    StrictEqual,

    // Equality (MSTest naming)
    AreEqual,
    AreNotEqual,
    AreSame,
    AreNotSame,

    // Boolean (xUnit naming)
    True,
    False,

    // Boolean (MSTest naming)
    IsTrue,
    IsFalse,

    // Null (xUnit naming)
    Null,
    NotNull,

    // Null (MSTest naming)
    IsNull,
    IsNotNull,

    // Collections (xUnit)
    Empty,
    NotEmpty,
    Single,
    Contains,
    DoesNotContain,
    All,

    // Collections (MSTest CollectionAssert)
    CollectionAreEqual,
    CollectionAreNotEqual,
    CollectionAreEquivalent,
    CollectionAreNotEquivalent,
    CollectionContains,
    CollectionDoesNotContain,
    CollectionIsSubsetOf,
    CollectionIsNotSubsetOf,
    CollectionAllItemsAreUnique,
    CollectionAllItemsAreNotNull,
    CollectionAllItemsAreInstancesOfType,

    // Exceptions
    Throws,
    ThrowsAsync,
    ThrowsAny,
    ThrowsAnyAsync,
    ThrowsException,

    // Type checks
    IsType,
    IsNotType,
    IsAssignableFrom,
    IsInstanceOfType,
    IsNotInstanceOfType,

    // String (xUnit)
    StartsWith,
    EndsWith,
    Matches,

    // String (MSTest StringAssert)
    StringContains,
    StringStartsWith,
    StringEndsWith,
    StringMatches,
    StringDoesNotMatch,

    // Comparison
    InRange,
    NotInRange,

    // Other
    Fail,
    Skip,
    Inconclusive,
    Collection,
    PropertyChanged,

    // Fallback
    Unknown
}

/// <summary>
/// Represents an attribute to convert
/// </summary>
public class AttributeConversion : ConversionTarget
{
    /// <summary>
    /// The new attribute name
    /// </summary>
    public required string NewAttributeName { get; init; }

    /// <summary>
    /// The new argument list (null to keep original, empty string to remove)
    /// </summary>
    public string? NewArgumentList { get; init; }

    /// <summary>
    /// Additional attributes to add alongside this conversion (e.g., [Skip] from [Fact(Skip = "reason")])
    /// </summary>
    public List<AdditionalAttribute>? AdditionalAttributes { get; init; }
}

/// <summary>
/// Represents an additional attribute to add during conversion
/// </summary>
public class AdditionalAttribute
{
    /// <summary>
    /// The attribute name (e.g., "Skip")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The attribute arguments (e.g., "(\"reason\")")
    /// </summary>
    public string? Arguments { get; init; }
}

/// <summary>
/// Represents an attribute to remove entirely
/// </summary>
public class AttributeRemoval : ConversionTarget
{
}

/// <summary>
/// Represents a base type to remove
/// </summary>
public class BaseTypeRemoval : ConversionTarget
{
    /// <summary>
    /// The base type name being removed (for logging)
    /// </summary>
    public required string TypeName { get; init; }
}

/// <summary>
/// Represents a method signature that needs async/Task added
/// </summary>
public class MethodSignatureChange : ConversionTarget
{
    /// <summary>
    /// Whether to add async modifier
    /// </summary>
    public bool AddAsync { get; init; }

    /// <summary>
    /// Whether to change return type to Task
    /// </summary>
    public bool ChangeReturnTypeToTask { get; init; }

    /// <summary>
    /// Whether to change return type to ValueTask
    /// </summary>
    public bool ChangeReturnTypeToValueTask { get; init; }

    /// <summary>
    /// Whether to change return type from ValueTask to Task
    /// </summary>
    public bool ChangeValueTaskToTask { get; init; }

    /// <summary>
    /// Whether to make the method public (for lifecycle methods)
    /// </summary>
    public bool MakePublic { get; init; }
}

/// <summary>
/// Represents a member (field, property, method) to remove
/// </summary>
public class MemberRemoval : ConversionTarget
{
    /// <summary>
    /// The member name being removed (for logging)
    /// </summary>
    public required string MemberName { get; init; }
}

/// <summary>
/// Represents a constructor parameter to remove
/// </summary>
public class ConstructorParameterRemoval : ConversionTarget
{
    /// <summary>
    /// The parameter name being removed
    /// </summary>
    public required string ParameterName { get; init; }

    /// <summary>
    /// The parameter type being removed
    /// </summary>
    public required string ParameterType { get; init; }
}

/// <summary>
/// Represents a failure during analysis
/// </summary>
public class ConversionFailure
{
    /// <summary>
    /// The phase where the failure occurred
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>
    /// Description of what failed
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The original code that couldn't be converted
    /// </summary>
    public string? OriginalCode { get; init; }

    /// <summary>
    /// The exception that caused the failure (if any)
    /// </summary>
    public Exception? Exception { get; init; }
}

/// <summary>
/// Represents a base type (interface) to add to a class
/// </summary>
public class BaseTypeAddition : ConversionTarget
{
    /// <summary>
    /// The interface name to add (e.g., "IAsyncInitializer")
    /// </summary>
    public required string TypeName { get; init; }
}

/// <summary>
/// Represents an attribute to add to a class
/// </summary>
public class ClassAttributeAddition : ConversionTarget
{
    /// <summary>
    /// The attribute code to add (e.g., "ClassDataSource<MyFixture>(Shared = SharedType.PerClass)")
    /// </summary>
    public required string AttributeCode { get; init; }
}

/// <summary>
/// Represents an attribute to add to a method
/// </summary>
public class MethodAttributeAddition : ConversionTarget
{
    /// <summary>
    /// The attribute code to add (e.g., "Before(Test)")
    /// </summary>
    public required string AttributeCode { get; init; }

    /// <summary>
    /// Whether to change the return type from Task to Task (keep as is) or apply other changes
    /// </summary>
    public string? NewReturnType { get; init; }
}

/// <summary>
/// Represents a Record.Exception call that needs to be converted to try-catch
/// </summary>
public class RecordExceptionConversion : ConversionTarget
{
    /// <summary>
    /// The variable name to assign the exception to (e.g., "ex")
    /// </summary>
    public required string VariableName { get; init; }

    /// <summary>
    /// The body of the lambda to execute in the try block
    /// </summary>
    public required string TryBlockBody { get; init; }
}

/// <summary>
/// Represents an invocation to replace (e.g., _testOutputHelper.WriteLine → Console.WriteLine)
/// </summary>
public class InvocationReplacement : ConversionTarget
{
    /// <summary>
    /// The new invocation code (e.g., "Console.WriteLine(args)")
    /// </summary>
    public required string ReplacementCode { get; init; }
}

/// <summary>
/// Represents a TheoryData field/property that needs to be converted to IEnumerable.
/// This handles both the type declaration and the object creation expression.
/// </summary>
public class TheoryDataConversion : ConversionTarget
{
    /// <summary>
    /// The element type(s) from TheoryData&lt;T&gt; (e.g., "TimeSpan" from TheoryData&lt;TimeSpan&gt;)
    /// </summary>
    public required string ElementType { get; init; }

    /// <summary>
    /// Annotation for the GenericName (TheoryData&lt;T&gt;) type syntax to convert to IEnumerable&lt;T&gt;
    /// </summary>
    public SyntaxAnnotation? TypeAnnotation { get; init; }

    /// <summary>
    /// Annotation for the object creation expression to convert to array creation
    /// </summary>
    public SyntaxAnnotation? CreationAnnotation { get; init; }
}

/// <summary>
/// Represents a parameter attribute to convert (e.g., [Range(1, 5)] → [MatrixRange&lt;int&gt;(1, 5)])
/// </summary>
public class ParameterAttributeConversion : ConversionTarget
{
    /// <summary>
    /// The new attribute name (e.g., "MatrixRange&lt;int&gt;")
    /// </summary>
    public required string NewAttributeName { get; init; }

    /// <summary>
    /// The new argument list (null to keep original, empty string to remove)
    /// </summary>
    public string? NewArgumentList { get; init; }
}
