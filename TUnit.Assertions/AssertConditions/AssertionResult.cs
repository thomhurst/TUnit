using System.Runtime.CompilerServices;
using System.Text;

namespace TUnit.Assertions.AssertConditions;

public class AssertionResult
{
    public bool IsPassed { get; }
    public string Message { get; }

    private AssertionResult(bool isPassed, string message)
    {
        IsPassed = isPassed;
        Message = message;
    }

    public static AssertionResult FailIf(bool isFailed, string message)
    {
        if (!isFailed)
        {
            return Passed;
        }
        
        return new AssertionResult(false, message);
    }
    
    public static AssertionResult FailIf(bool isFailed, [InterpolatedStringHandlerArgument("isFailed")] InterpolatedStringHandler stringHandler)
    {
        if (!isFailed)
        {
            return Passed;
        }
        
        return new AssertionResult(false, stringHandler.GetFormattedText());
    }

    public AssertionResult And(AssertionResult other)
    {
        if (IsPassed && other.IsPassed)
        {
            return this;
        }

        if (IsPassed)
        {
            return other;
        }

        if (other.IsPassed)
        {
            return this;
        }
        
        return Message == other.Message 
            ? Fail(Message) 
            : Fail(Message + " and " + other.Message);
    }

    // TODO: Should this be removed/obsoleted?
    public AssertionResult Or(AssertionResult other)
    {
        if (!IsPassed && !other.IsPassed)
        {
            return Message == other.Message 
                ? Fail(Message) 
                : Fail(Message + " and " + other.Message);
        }

        return Passed;
    }

    public async ValueTask<AssertionResult> OrAsync(Func<ValueTask<AssertionResult>> otherResult)
    {
        if (IsPassed)
        {
            return Passed;
        }

        var other = await otherResult();
        if (other.IsPassed)
        {
            return Passed;
        }

        return Message == other.Message 
                   ? Fail(Message) 
                   : Fail(Message + " and " + other.Message);
    }

    public AssertionResult OrFailIf(bool isFailed, string message)
    {
        if (!IsPassed || !isFailed)
        {
            return this;
        }
        
        return new AssertionResult(false, message);
    }
    
    public AssertionResult OrFailIf(bool isFailed, [InterpolatedStringHandlerArgument("isFailed")] InterpolatedStringHandler stringHandler)
    {
        if (!IsPassed || !isFailed)
        {
            return this;
        }
        
        return new AssertionResult(false, stringHandler.GetFormattedText());
    }

    public static AssertionResult Fail(string message)
        => new(false, message);

    public static AssertionResult Passed { get; } = new(true, string.Empty);
    
    public static implicit operator Task<AssertionResult>(AssertionResult result) => Task.FromResult(result);
    public static implicit operator ValueTask<AssertionResult>(AssertionResult result) => new(result);

    [InterpolatedStringHandler]
    public readonly struct InterpolatedStringHandler
    {
        private readonly StringBuilder _builder;

        public InterpolatedStringHandler(int literalLength, int formattedCount, bool isFailed, out bool enabled)
        {
            enabled = isFailed;
            
            _builder = enabled ? new StringBuilder(literalLength) : null!;
        }
        
        public void AppendLiteral(string s)
        {
            _builder.Append(s);
        }

        public void AppendFormatted<T>(T? t)
        {
            _builder.Append(t);
        }
        
        public void AppendFormatted<T>(T? t, string format) where T : IFormattable
        {
            _builder.Append(t?.ToString(format, null));
        }

        internal string GetFormattedText()
        {
            return _builder?.ToString() ?? string.Empty;
        }
    }
}