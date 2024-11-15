namespace TUnit.Assertions.AssertConditions;

public class AssertionResult
{
    public bool IsPassed { get; }
    public string Message
    {
        get
        {
            return _message.Value;
        }
    }
    private readonly Lazy<string> _message;

    private AssertionResult(bool isPassed, Func<string> messageGenerator)
    {
        IsPassed = isPassed;
        _message = new Lazy<string>(messageGenerator);
    }

    public static AssertionResult FailIf(Func<bool> isFailed, Func<string> messageGenerator)
    {
        if (!isFailed())
        {
            return Passed;
        }
        
        return new AssertionResult(false, messageGenerator);
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

        return Fail(() =>
        {
            if (Message == other.Message)
            {
                return Message;
            }

            return Message + " and " + other.Message;
        });
    }

    public AssertionResult Or(AssertionResult other)
    {
        if (!IsPassed && !other.IsPassed)
        {
            return Fail(() =>
            {
                if (Message == other.Message)
                {
                    return Message;
                }

                return Message + " and " + other.Message;
            });
        }

        return Passed;
    }

    public AssertionResult OrFailIf(Func<bool> isFailed, Func<string> message)
    {
        if (!IsPassed || !isFailed())
        {
            return this;
        }
        
        return new AssertionResult(false, message);
    }

    public static AssertionResult Fail(Func<string> message)
        => new(false, message);

    public static AssertionResult Passed { get; } = new(true, () => string.Empty);
    
    public static implicit operator Task<AssertionResult>(AssertionResult result) => Task.FromResult(result);
}