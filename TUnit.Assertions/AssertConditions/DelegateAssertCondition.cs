﻿namespace TUnit.Assertions.AssertConditions;

public abstract class DelegateAssertCondition : DelegateAssertCondition<object?, Exception>;

public abstract class ExpectedExceptionDelegateAssertCondition<TException> : DelegateAssertCondition<object?, Exception>;

public abstract class DelegateAssertCondition<TActual> : DelegateAssertCondition<TActual, Exception>;

public abstract class DelegateAssertCondition<TActual, TException> : BaseAssertCondition<TActual> where TException : Exception
{
    private readonly List<Func<TException?, AssertionDecision>> _customComparers = [];

    public void WithComparer(Func<TException?, AssertionDecision> comparer)
    {
        _customComparers.Add(comparer);
    }

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (exception != null && exception is not TException)
        {
            return FailWithMessage($"Expected type {typeof(TException).Name} but was {exception.GetType().Name}");
        }

        var typedException = exception as TException;

        foreach (var result in _customComparers.Select(customComparer => customComparer(typedException)))
        {
            switch (result)
            {
                case AssertionDecision.PassDecision:
                    return true;
                case AssertionDecision.FailDecision failDecision:
                    return FailWithMessage(failDecision.Message);
            }
        }
        
        return Passes(typedException);
    }

    protected abstract bool Passes(TException? exception);
    
    protected abstract string GetFailureMessage(TException? exception);

    protected internal override string GetFailureMessage()
    {
        return GetFailureMessage(Exception as TException);
    }
}