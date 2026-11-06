using System;
using System.Threading.Tasks;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

// Test case: XML documentation on [AssertionExtension] classes. Users only see the generated
// extension methods, so a documented class passes its summary, remarks and type parameters on to
// them, and a documented constructor its parameters (except the context) and exceptions.

/// <summary>
/// Checks that the value lies in a range.
/// </summary>
/// <remarks>Both bounds are inclusive.</remarks>
[AssertionExtension("IsInDocumentedRange")]
public class DocumentedRangeAssertion : Assertion<int>
{
    /// <summary>
    /// Creates the assertion for a range.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <exception cref="ArgumentException">The minimum is greater than the maximum.</exception>
    public DocumentedRangeAssertion(AssertionContext<int> context, int minimum, int maximum)
        : base(context)
    {
        if (minimum > maximum)
        {
            throw new ArgumentException("The minimum is greater than the maximum.", nameof(minimum));
        }
    }

    /// <summary>
    /// Creates the assertion for a range starting at zero.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    public DocumentedRangeAssertion(AssertionContext<int> context, int maximum)
        : this(context, 0, maximum)
    {
    }

    /// <summary>
    /// Not public, so it gets no extension method.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    internal DocumentedRangeAssertion(AssertionContext<int> context, long maximum)
        : this(context, 0, (int)maximum)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to be in range";
}

public class DocumentedSubject
{
}

/// <summary>
/// An internal type, which a package still exposes.
/// </summary>
internal static class DocumentedInternalType
{
}

/// <summary>
/// Checks that the subject carries a tag.
/// </summary>
/// <typeparam name="TTag">The type of the tag.</typeparam>
[AssertionExtension("HasDocumentedTag")]
public class DocumentedTagAssertion<TTag> : Assertion<DocumentedSubject>
{
    /// <summary>
    /// Creates the assertion.
    /// </summary>
    /// <param name="assertionContext">The assertion context.</param>
    /// <param name="tag">The tag to look for.</param>
    public DocumentedTagAssertion(AssertionContext<DocumentedSubject> assertionContext, TTag tag)
        : base(assertionContext)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DocumentedSubject> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to carry the tag";
}

/// <summary>
/// Checks that the subject carries a tag of a type.
/// </summary>
/// <typeparam name="TTag">The type of the tag.</typeparam>
[AssertionExtension("HasDocumentedTagOfType")]
public class DocumentedTagOfTypeAssertion<TTag> : Assertion<DocumentedSubject>
{
    /// <summary>
    /// Creates the assertion.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    public DocumentedTagOfTypeAssertion(AssertionContext<DocumentedSubject> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DocumentedSubject> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to carry a tag of the type";
}

/// <summary>
/// Checks that the text is documented on the class only.
/// </summary>
[AssertionExtension("IsClassOnlyDocumented")]
public class ClassOnlyDocumentedAssertion : Assertion<string>
{
    public ClassOnlyDocumentedAssertion(AssertionContext<string> context, string expected)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to be documented on the class";
}

[AssertionExtension("IsUndocumented")]
public class UndocumentedAssertion : Assertion<string>
{
    public UndocumentedAssertion(AssertionContext<string> context, string expected)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to be undocumented";
}

[AssertionExtension("IsConstructorOnlyDocumented")]
public class ConstructorOnlyDocumentedAssertion : Assertion<string>
{
    /// <summary>
    /// Creates the assertion.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    /// <param name="expected">The expected text.</param>
    public ConstructorOnlyDocumentedAssertion(AssertionContext<string> context, string expected)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to be documented on the constructor";
}

/// <summary>
/// Checks that the value is true.
/// </summary>
[AssertionExtension("IsDocumentedTrue", NegatedMethodName = "IsDocumentedFalse")]
public class NegatableDocumentedAssertion : Assertion<bool>
{
    /// <summary>
    /// Creates the assertion.
    /// </summary>
    /// <param name="context">The assertion context.</param>
    public NegatableDocumentedAssertion(AssertionContext<bool> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<bool> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to be true";
}
