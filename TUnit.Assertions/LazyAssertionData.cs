using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public abstract class LazyAssertionData
{
    private readonly string? _actualExpression;

    private AssertionData? _evaluatedAssertionData;

    private LazyAssertionData(string? actualExpression)
    {
        _actualExpression = actualExpression;
    }

    public async ValueTask<AssertionData> GetResultAsync()
    {
        if (_evaluatedAssertionData is { } alreadyEvaluated)
        {
            return alreadyEvaluated;
        }

        var evaluated = await EvaluateAsync();
        _evaluatedAssertionData = evaluated;
        return evaluated;
    }

    protected abstract ValueTask<AssertionData> EvaluateAsync();

    public static LazyAssertionData Create(Action action, string? actualExpression) =>
        new ForAction(action, actualExpression);

    public static LazyAssertionData Create(Func<Task> action, string? actualExpression) =>
        new ForAsyncAction(action, actualExpression);

    public static LazyAssertionData Create<T>(Func<Task<T>> func, string? actualExpression) =>
        new ForAsyncFunc<T>(func, actualExpression);

    public static LazyAssertionData Create<T>(Func<T> func, string? actualExpression) =>
        new ForFunc<T>(func, actualExpression);

    public static LazyAssertionData Create<T>(T value, string? actualExpression) =>
        new ForValue<T>(value, actualExpression);

    public LazyAssertionData WithExceptionAsValue<TException>(IDelegateSource delegateSource, ConvertExceptionToValueAssertCondition<TException> convertToAssertCondition) where TException : Exception =>
        new ForExceptionAsValue<TException>(this, delegateSource, convertToAssertCondition);

    public LazyAssertionData WithConversion<TFromType, TToType>(IValueSource<TFromType> valueSource,
        ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition) =>
        new ForConversion<TFromType, TToType>(this, valueSource, convertToAssertCondition);

    private sealed class ForAction(Action action, string? actualExpression)
        : LazyAssertionData(actualExpression)
    {
        protected override async ValueTask<AssertionData> EvaluateAsync()
        {
            var start = DateTimeOffset.Now;

            try
            {
                await Task.Factory.StartNew(action);

                var end = DateTimeOffset.Now;

                return (null, null, _actualExpression, start, end);
            }
            catch (Exception e)
            {
                var end = DateTimeOffset.Now;

                return (null, e, _actualExpression, start, end);
            }
        }
    }

    private sealed class ForAsyncAction(Func<Task> action, string? actualExpression)
        : LazyAssertionData(actualExpression)
    {
        protected override async ValueTask<AssertionData> EvaluateAsync()
        {
            var start = DateTimeOffset.Now;

            try
            {
                await action();

                var end = DateTimeOffset.Now;

                return (null, null, _actualExpression, start, end);
            }
            catch (Exception e)
            {
                var end = DateTimeOffset.Now;

                return (null, e, _actualExpression, start, end);
            }
        }
    }

    private sealed class ForFunc<T>(Func<T> func, string? actualExpression)
        : LazyAssertionData(actualExpression)
    {
        protected override async ValueTask<AssertionData> EvaluateAsync()
        {
            var start = DateTimeOffset.Now;

            try
            {
                var result = await Task.Factory.StartNew(func);

                var end = DateTimeOffset.Now;

                return (result, null, _actualExpression, start, end);
            }
            catch (Exception e)
            {
                var end = DateTimeOffset.Now;

                return (null, e, _actualExpression, start, end);
            }
        }
    }

    private sealed class ForAsyncFunc<T>(Func<Task<T>> func, string? actualExpression)
        : LazyAssertionData(actualExpression)
    {
        protected override async ValueTask<AssertionData> EvaluateAsync()
        {
            var start = DateTimeOffset.Now;

            try
            {
                var result = await func();

                var end = DateTimeOffset.Now;

                return (result, null, _actualExpression, start, end);
            }
            catch (Exception e)
            {
                var end = DateTimeOffset.Now;

                return (null, e, _actualExpression, start, end);
            }
        }
    }

    private sealed class ForValue<T>(T value, string? actualExpression)
        : LazyAssertionData(actualExpression)
    {
        protected override ValueTask<AssertionData> EvaluateAsync()
        {
            var now = DateTimeOffset.Now;
            return new ValueTask<AssertionData>(new AssertionData(value, null, _actualExpression, now, now));
        }
    }

    private sealed class ForExceptionAsValue<TException>(LazyAssertionData inner, IDelegateSource delegateSource, ConvertExceptionToValueAssertCondition<TException> convertToAssertCondition)
        : LazyAssertionData(inner._actualExpression)
        where TException : Exception
    {
        protected override async ValueTask<AssertionData> EvaluateAsync()
        {
            var invokableAssertionBuilder = delegateSource.RegisterAssertion(convertToAssertCondition, [], null);

            return await invokableAssertionBuilder.ProcessAssertionsAsync(assertionData =>
                Task.FromResult(assertionData with { Result = convertToAssertCondition.ConvertedExceptionValue, Exception = null, End = DateTimeOffset.Now }));
        }
    }

    private sealed class ForConversion<TFromType, TToType>(LazyAssertionData inner, IValueSource<TFromType> valueSource, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
        : LazyAssertionData(inner._actualExpression)
    {
        protected override async ValueTask<AssertionData> EvaluateAsync()
        {
            var invokableAssertionBuilder = valueSource.RegisterAssertion(convertToAssertCondition, [], null);

            return await invokableAssertionBuilder.ProcessAssertionsAsync(assertionData =>
                Task.FromResult(assertionData with { Result = convertToAssertCondition.ConvertedValue, End = DateTimeOffset.Now }));
        }
    }
}
