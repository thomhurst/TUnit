using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class StringLength<TAnd, TOr> : Connector<string, TAnd, TOr>
    where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
    where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
{
    protected AssertionBuilder<string> AssertionBuilder { get; }

    public StringLength(AssertionBuilder<string> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<string, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Length");
    }

    public BaseAssertCondition<string, TAnd, TOr> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (actual, _, _, self) =>
            {
                if (actual is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return actual.Length == expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {expected}")
        );
    }

    public BaseAssertCondition<string, TAnd, TOr> Zero =>
        Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), 0, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return @string.Length == 0;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {0}")
        );

    public BaseAssertCondition<string, TAnd, TOr> Positive =>
        Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), default, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return @string.Length > 0;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to empty"
        ));


    public BaseAssertCondition<string, TAnd, TOr> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(
            AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return @string.Length > expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than {expected}")
        );
    }

    public BaseAssertCondition<string, TAnd, TOr> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return @string.Length >= expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than or equal to {expected}")
        );
    }

    public BaseAssertCondition<string, TAnd, TOr> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return @string.Length < expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than {expected}")
        );
    }

    public BaseAssertCondition<string, TAnd, TOr> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return @string.Length <= expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than or equal to {expected}")
        );
    }
}