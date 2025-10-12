using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

[AssertionExtension("IsLetter")]
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

[AssertionExtension("IsNotLetter")]
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

[AssertionExtension("IsDigit")]
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

[AssertionExtension("IsNotDigit")]
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

[AssertionExtension("IsWhiteSpace")]
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

[AssertionExtension("IsNotWhiteSpace")]
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

[AssertionExtension("IsUpper")]
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

[AssertionExtension("IsNotUpper")]
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

[AssertionExtension("IsLower")]
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

[AssertionExtension("IsNotLower")]
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

[AssertionExtension("IsControl")]
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

[AssertionExtension("IsNotControl")]
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

[AssertionExtension("IsPunctuation")]
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

[AssertionExtension("IsNotPunctuation")]
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

[AssertionExtension("IsSymbol")]
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

[AssertionExtension("IsNotSymbol")]
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

[AssertionExtension("IsNumber")]
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

[AssertionExtension("IsNotNumber")]
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

[AssertionExtension("IsSeparator")]
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

[AssertionExtension("IsNotSeparator")]
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

[AssertionExtension("IsSurrogate")]
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

[AssertionExtension("IsNotSurrogate")]
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

[AssertionExtension("IsHighSurrogate")]
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

[AssertionExtension("IsNotHighSurrogate")]
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

[AssertionExtension("IsLowSurrogate")]
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

[AssertionExtension("IsNotLowSurrogate")]
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

[AssertionExtension("IsLetterOrDigit")]
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

[AssertionExtension("IsNotLetterOrDigit")]
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
