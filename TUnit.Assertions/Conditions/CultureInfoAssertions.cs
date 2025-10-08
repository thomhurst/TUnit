using System.Globalization;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

public class IsInvariantCultureAssertion : Assertion<CultureInfo>
{
    public IsInvariantCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null || !value.Equals(CultureInfo.InvariantCulture))
            return Task.FromResult(AssertionResult.Failed($"culture was {value?.Name ?? "null"}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be invariant culture";
}

public class IsNotInvariantCultureAssertion : Assertion<CultureInfo>
{
    public IsNotInvariantCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != null && value.Equals(CultureInfo.InvariantCulture))
            return Task.FromResult(AssertionResult.Failed("culture was invariant"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be invariant culture";
}

public class IsNeutralCultureAssertion : Assertion<CultureInfo>
{
    public IsNeutralCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null || !value.IsNeutralCulture)
            return Task.FromResult(AssertionResult.Failed($"culture {value?.Name ?? "null"} is not neutral"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a neutral culture";
}

public class IsNotNeutralCultureAssertion : Assertion<CultureInfo>
{
    public IsNotNeutralCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != null && value.IsNeutralCulture)
            return Task.FromResult(AssertionResult.Failed($"culture {value.Name} is neutral"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be a neutral culture";
}

public class IsEnglishCultureAssertion : Assertion<CultureInfo>
{
    public IsEnglishCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null || value.TwoLetterISOLanguageName != "en")
            return Task.FromResult(AssertionResult.Failed($"culture was {value?.Name ?? "null"} (language code: {value?.TwoLetterISOLanguageName ?? "null"})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be English culture";
}

public class IsNotEnglishCultureAssertion : Assertion<CultureInfo>
{
    public IsNotEnglishCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != null && value.TwoLetterISOLanguageName == "en")
            return Task.FromResult(AssertionResult.Failed($"culture {value.Name} is English"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be English culture";
}

public class IsRightToLeftCultureAssertion : Assertion<CultureInfo>
{
    public IsRightToLeftCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null || !value.TextInfo.IsRightToLeft)
            return Task.FromResult(AssertionResult.Failed($"culture {value?.Name ?? "null"} is not right-to-left"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be right-to-left culture";
}

public class IsLeftToRightCultureAssertion : Assertion<CultureInfo>
{
    public IsLeftToRightCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != null && value.TextInfo.IsRightToLeft)
            return Task.FromResult(AssertionResult.Failed($"culture {value.Name} is right-to-left"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be left-to-right culture";
}

public class IsReadOnlyCultureAssertion : Assertion<CultureInfo>
{
    public IsReadOnlyCultureAssertion(
        EvaluationContext<CultureInfo> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(CultureInfo? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null || !value.IsReadOnly)
            return Task.FromResult(AssertionResult.Failed($"culture {value?.Name ?? "null"} is not read-only"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be read-only culture";
}
