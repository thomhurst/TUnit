using System.Globalization;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

[AssertionExtension("IsInvariantCulture")]
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

[AssertionExtension("IsNotInvariantCulture")]
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

[AssertionExtension("IsNeutralCulture")]
public class IsNeutralCultureAssertion : Assertion<CultureInfo>
{
    public IsNeutralCultureAssertion(
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

        if (value == null || !value.IsNeutralCulture)
        {
            return Task.FromResult(AssertionResult.Failed($"culture {value?.Name ?? "null"} is not neutral"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a neutral culture";
}

[AssertionExtension("IsNotNeutralCulture")]
public class IsNotNeutralCultureAssertion : Assertion<CultureInfo>
{
    public IsNotNeutralCultureAssertion(
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

        if (value != null && value.IsNeutralCulture)
        {
            return Task.FromResult(AssertionResult.Failed($"culture {value.Name} is neutral"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be a neutral culture";
}

[AssertionExtension("IsEnglishCulture")]
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

[AssertionExtension("IsNotEnglishCulture")]
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

[AssertionExtension("IsRightToLeftCulture")]
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

[AssertionExtension("IsLeftToRightCulture")]
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

[AssertionExtension("IsReadOnlyCulture")]
public class IsReadOnlyCultureAssertion : Assertion<CultureInfo>
{
    public IsReadOnlyCultureAssertion(
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

        if (value == null || !value.IsReadOnly)
        {
            return Task.FromResult(AssertionResult.Failed($"culture {value?.Name ?? "null"} is not read-only"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be read-only culture";
}
