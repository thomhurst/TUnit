using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

public class IsUTF8EncodingAssertion : Assertion<Encoding>
{
    public IsUTF8EncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.Equals(Encoding.UTF8))
        {
            return Task.FromResult(AssertionResult.Failed($"encoding was {value?.EncodingName ?? "null"}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be UTF-8 encoding";
}

public class IsNotUTF8EncodingAssertion : Assertion<Encoding>
{
    public IsNotUTF8EncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != null && value.Equals(Encoding.UTF8))
        {
            return Task.FromResult(AssertionResult.Failed("encoding was UTF-8"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be UTF-8 encoding";
}

public class IsASCIIEncodingAssertion : Assertion<Encoding>
{
    public IsASCIIEncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.Equals(Encoding.ASCII))
        {
            return Task.FromResult(AssertionResult.Failed($"encoding was {value?.EncodingName ?? "null"}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be ASCII encoding";
}

public class IsUnicodeEncodingAssertion : Assertion<Encoding>
{
    public IsUnicodeEncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.Equals(Encoding.Unicode))
        {
            return Task.FromResult(AssertionResult.Failed($"encoding was {value?.EncodingName ?? "null"}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be Unicode encoding";
}

public class IsUTF32EncodingAssertion : Assertion<Encoding>
{
    public IsUTF32EncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.Equals(Encoding.UTF32))
        {
            return Task.FromResult(AssertionResult.Failed($"encoding was {value?.EncodingName ?? "null"}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be UTF-32 encoding";
}

public class IsBigEndianUnicodeEncodingAssertion : Assertion<Encoding>
{
    public IsBigEndianUnicodeEncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.Equals(Encoding.BigEndianUnicode))
        {
            return Task.FromResult(AssertionResult.Failed($"encoding was {value?.EncodingName ?? "null"}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be big-endian Unicode encoding";
}

public class IsSingleByteEncodingAssertion : Assertion<Encoding>
{
    public IsSingleByteEncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null || !value.IsSingleByte)
        {
            return Task.FromResult(AssertionResult.Failed($"encoding {value?.EncodingName ?? "null"} is not single-byte"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be single-byte encoding";
}

public class IsNotSingleByteEncodingAssertion : Assertion<Encoding>
{
    public IsNotSingleByteEncodingAssertion(
        AssertionContext<Encoding> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Encoding> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != null && value.IsSingleByte)
        {
            return Task.FromResult(AssertionResult.Failed($"encoding {value.EncodingName} is single-byte"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be single-byte encoding";
}
