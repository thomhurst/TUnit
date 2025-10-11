using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

public class IsLetterAssertion : Assertion<char>
{
    public IsLetterAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a letter";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsLetter(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a letter, but was '{metadata.Value}'"));
    }
}

public class IsNotLetterAssertion : Assertion<char>
{
    public IsNotLetterAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a letter";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsLetter(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a letter, but was '{metadata.Value}'"));
    }
}

public class IsDigitAssertion : Assertion<char>
{
    public IsDigitAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a digit";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsDigit(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a digit, but was '{metadata.Value}'"));
    }
}

public class IsNotDigitAssertion : Assertion<char>
{
    public IsNotDigitAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a digit";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsDigit(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a digit, but was '{metadata.Value}'"));
    }
}

public class IsWhiteSpaceAssertion : Assertion<char>
{
    public IsWhiteSpaceAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be whitespace";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsWhiteSpace(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be whitespace, but was '{metadata.Value}'"));
    }
}

public class IsNotWhiteSpaceAssertion : Assertion<char>
{
    public IsNotWhiteSpaceAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be whitespace";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsWhiteSpace(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be whitespace, but was '{metadata.Value}'"));
    }
}

public class IsUpperAssertion : Assertion<char>
{
    public IsUpperAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be uppercase";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsUpper(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be uppercase, but was '{metadata.Value}'"));
    }
}

public class IsNotUpperAssertion : Assertion<char>
{
    public IsNotUpperAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be uppercase";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsUpper(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be uppercase, but was '{metadata.Value}'"));
    }
}

public class IsLowerAssertion : Assertion<char>
{
    public IsLowerAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be lowercase";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsLower(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be lowercase, but was '{metadata.Value}'"));
    }
}

public class IsNotLowerAssertion : Assertion<char>
{
    public IsNotLowerAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be lowercase";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsLower(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be lowercase, but was '{metadata.Value}'"));
    }
}

public class IsControlAssertion : Assertion<char>
{
    public IsControlAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a control character";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsControl(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a control character, but was '{metadata.Value}'"));
    }
}

public class IsNotControlAssertion : Assertion<char>
{
    public IsNotControlAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a control character";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsControl(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a control character, but was '{metadata.Value}'"));
    }
}

public class IsPunctuationAssertion : Assertion<char>
{
    public IsPunctuationAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be punctuation";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsPunctuation(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be punctuation, but was '{metadata.Value}'"));
    }
}

public class IsNotPunctuationAssertion : Assertion<char>
{
    public IsNotPunctuationAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be punctuation";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsPunctuation(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be punctuation, but was '{metadata.Value}'"));
    }
}

public class IsSymbolAssertion : Assertion<char>
{
    public IsSymbolAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a symbol";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsSymbol(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a symbol, but was '{metadata.Value}'"));
    }
}

public class IsNotSymbolAssertion : Assertion<char>
{
    public IsNotSymbolAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a symbol";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsSymbol(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a symbol, but was '{metadata.Value}'"));
    }
}

public class IsNumberAssertion : Assertion<char>
{
    public IsNumberAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a number";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsNumber(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a number, but was '{metadata.Value}'"));
    }
}

public class IsNotNumberAssertion : Assertion<char>
{
    public IsNotNumberAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a number";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsNumber(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a number, but was '{metadata.Value}'"));
    }
}

public class IsSeparatorAssertion : Assertion<char>
{
    public IsSeparatorAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a separator";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsSeparator(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a separator, but was '{metadata.Value}'"));
    }
}

public class IsNotSeparatorAssertion : Assertion<char>
{
    public IsNotSeparatorAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a separator";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsSeparator(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a separator, but was '{metadata.Value}'"));
    }
}

public class IsSurrogateAssertion : Assertion<char>
{
    public IsSurrogateAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a surrogate";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsSurrogate(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a surrogate, but was '{metadata.Value}'"));
    }
}

public class IsNotSurrogateAssertion : Assertion<char>
{
    public IsNotSurrogateAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a surrogate";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsSurrogate(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a surrogate, but was '{metadata.Value}'"));
    }
}

public class IsHighSurrogateAssertion : Assertion<char>
{
    public IsHighSurrogateAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a high surrogate";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsHighSurrogate(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a high surrogate, but was '{metadata.Value}'"));
    }
}

public class IsNotHighSurrogateAssertion : Assertion<char>
{
    public IsNotHighSurrogateAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a high surrogate";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsHighSurrogate(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a high surrogate, but was '{metadata.Value}'"));
    }
}

public class IsLowSurrogateAssertion : Assertion<char>
{
    public IsLowSurrogateAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a low surrogate";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsLowSurrogate(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a low surrogate, but was '{metadata.Value}'"));
    }
}

public class IsNotLowSurrogateAssertion : Assertion<char>
{
    public IsNotLowSurrogateAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a low surrogate";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsLowSurrogate(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a low surrogate, but was '{metadata.Value}'"));
    }
}

public class IsLetterOrDigitAssertion : Assertion<char>
{
    public IsLetterOrDigitAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to be a letter or digit";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (char.IsLetterOrDigit(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a letter or digit, but was '{metadata.Value}'"));
    }
}

public class IsNotLetterOrDigitAssertion : Assertion<char>
{
    public IsNotLetterOrDigitAssertion(AssertionContext<char> context) : base(context) { }
    protected override string GetExpectation() => "to not be a letter or digit";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<char> metadata)
    {
        if (!char.IsLetterOrDigit(metadata.Value))
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be a letter or digit, but was '{metadata.Value}'"));
    }
}
