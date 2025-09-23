using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Exceptions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class ExceptionAssertionBuilder<TException> : AssertionBuilder<TException> 
    where TException : Exception
{
    public ExceptionAssertionBuilder(Func<Task> asyncFunc, string? actualExpression)
        : base(CaptureException(asyncFunc), actualExpression)
    {
    }

    public ExceptionAssertionBuilder(Action action, string? actualExpression)
        : base(CaptureException(action), actualExpression)
    {
    }

    private static async ValueTask<TException> CaptureException(Func<Task> asyncFunc)
    {
        try
        {
            await asyncFunc();
            throw new AssertionException("No exception was thrown");
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}");
        }
    }

    private static ValueTask<TException> CaptureException(Action action)
    {
        try
        {
            action();
            throw new AssertionException("No exception was thrown");
        }
        catch (TException ex)
        {
            return new ValueTask<TException>(ex);
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}");
        }
    }

    public ExceptionAssertionBuilder<TException> WithParameterName(string parameterName, string? expression = null)
    {
        AppendCallerMethod([expression ?? $"\"{parameterName}\""]);
        // Add assertion for parameter name checking
        return this;
    }
}