using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class EnumerableCount<TAssertionBuilder, TOutput, TActual, TAnd, TOr>
    where TActual : IEnumerable?
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
    where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
{
    protected internal TAssertionBuilder AssertionBuilder { get; }

    public EnumerableCount(TAssertionBuilder assertionBuilder)
    {
        AssertionBuilder = (TAssertionBuilder) assertionBuilder.AppendExpression("Count");
    }

    public TOutput EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(AssertionBuilder);
    }

    public TOutput Empty =>
        (new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), 0, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {0}")
        )
        .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(AssertionBuilder);
    
    public TOutput GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int, TAnd, TOr>(
            AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (enumerable, _, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) > expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(AssertionBuilder);
    }

    public TOutput GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(AssertionBuilder);
    }

    public TOutput LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) < expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(AssertionBuilder);
    }

    public TOutput LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(AssertionBuilder);
    }
    
    public TOutput Negative() => LessThan(0);

    public TOutput EqualToZero() => EqualTo(0);
    public TOutput EqualToOne() => EqualTo(1);
    
    public TOutput Positive() => GreaterThan(0);


    private int GetCount(TActual? actualValue)
    {
        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }

        if (actualValue is IList list)
        {
            return list.Count;
        }

        return actualValue?.Cast<object>().Count() ?? 0;
    }
}