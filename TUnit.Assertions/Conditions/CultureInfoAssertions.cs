using System.Globalization;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

[AssertionExtension("IsInvariant")]
public class IsInvariantCultureAssertion : Assertion<CultureInfo>
{
    public IsInvariantCultureAssertion(
        AssertionContext<CultureInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CultureInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.Equals(CultureInfo.InvariantCulture))
        {
            return Task.FromResult(AssertionResult.Failed($"culture was {value?.Name ?? "null"}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be invariant culture";
}

[AssertionExtension("IsNotInvariant")]
public class IsNotInvariantCultureAssertion : Assertion<CultureInfo>
{
    public IsNotInvariantCultureAssertion(
        AssertionContext<CultureInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CultureInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != null && value.Equals(CultureInfo.InvariantCulture))
        {
            return Task.FromResult(AssertionResult.Failed("culture was invariant"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be invariant culture";
}

// NOTE: IsNeutralCulture and IsNotNeutralCulture have been migrated to source-generated assertions in CultureInfoPropertyAssertions.cs

[AssertionExtension("IsEnglish")]
public class IsEnglishCultureAssertion : Assertion<CultureInfo>
{
    public IsEnglishCultureAssertion(
        AssertionContext<CultureInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CultureInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || value.TwoLetterISOLanguageName != "en")
        {
            return Task.FromResult(AssertionResult.Failed($"culture was {value?.Name ?? "null"} (language code: {value?.TwoLetterISOLanguageName ?? "null"})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be English culture";
}

[AssertionExtension("IsNotEnglish")]
public class IsNotEnglishCultureAssertion : Assertion<CultureInfo>
{
    public IsNotEnglishCultureAssertion(
        AssertionContext<CultureInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CultureInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != null && value.TwoLetterISOLanguageName == "en")
        {
            return Task.FromResult(AssertionResult.Failed($"culture {value.Name} is English"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be English culture";
}

[AssertionExtension("IsRightToLeft")]
public class IsRightToLeftCultureAssertion : Assertion<CultureInfo>
{
    public IsRightToLeftCultureAssertion(
        AssertionContext<CultureInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CultureInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.TextInfo.IsRightToLeft)
        {
            return Task.FromResult(AssertionResult.Failed($"culture {value?.Name ?? "null"} is not right-to-left"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be right-to-left culture";
}

[AssertionExtension("IsLeftToRight")]
public class IsLeftToRightCultureAssertion : Assertion<CultureInfo>
{
    public IsLeftToRightCultureAssertion(
        AssertionContext<CultureInfo> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CultureInfo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != null && value.TextInfo.IsRightToLeft)
        {
            return Task.FromResult(AssertionResult.Failed($"culture {value.Name} is right-to-left"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be left-to-right culture";
}

// NOTE: IsReadOnly has been migrated to source-generated assertions in CultureInfoPropertyAssertions.cs
