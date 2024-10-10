﻿namespace TUnit.Assertions.AssertConditions;

public class AssertionResult
{
    public bool IsPassed { get; }
    public string? Message { get; }

    private AssertionResult(bool isPassed, string? message)
    {
        IsPassed = isPassed;
        Message = message;
    }

    public static AssertionResult FailIf(Func<bool> isFailed, string message)
    {
        if (!isFailed())
        {
            return Passed;
        }
        return new AssertionResult(false, message);
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

        return Fail(Message + " and " + other.Message);
    }

    public AssertionResult Or(AssertionResult other)
    {
        if (!IsPassed && !other.IsPassed)
        {
            return Fail(Message + " and " + other.Message);
        }

        return Passed;
    }

    public AssertionResult OrFailIf(Func<bool> isFailed, string message)
    {
        if (!IsPassed || !isFailed())
        {
            return this;
        }
        return new AssertionResult(false, message);
    }

    public static AssertionResult Fail(string message)
        => new AssertionResult(false, message);

    public static AssertionResult Passed { get; }
        = new AssertionResult(true, null);
}