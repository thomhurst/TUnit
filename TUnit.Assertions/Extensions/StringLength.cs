using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class StringLength<TAnd, TOr>
    where TAnd : IAnd<string, TAnd, TOr>
    where TOr : IOr<string, TAnd, TOr>
{
    protected AssertionBuilder<string, TAnd, TOr> AssertionBuilder { get; }

    public StringLength(AssertionBuilder<string, TAnd, TOr> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Length");
    }

    public InvokableAssertionBuilder<string, TAnd, TOr> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<string, int>(expected, (actual, _, _, self) =>
            {
                if (actual is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return actual.Length == expected;
            },
            (@string, _, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<string, TAnd, TOr> Zero =>
        new DelegateAssertCondition<string, int>(0,
                (@string, _, _, self) =>
                {
                    if (@string is null)
                    {
                        self.OverriddenMessage = "Actual string is null";
                        return false;
                    }

                    return @string.Length == 0;
                },
                (@string, _, _) =>
                    $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {0}")
            .ChainedTo(AssertionBuilder, []);

    public InvokableAssertionBuilder<string, TAnd, TOr> Positive =>
        new DelegateAssertCondition<string, int>(default, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return @string.Length > 0;
            },
            (@string, _, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to empty"
        ).ChainedTo(AssertionBuilder, []);


    public InvokableAssertionBuilder<string, TAnd, TOr> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<string, int>(expected,
            (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return @string.Length > expected;
            },
            (@string, _, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<string, TAnd, TOr> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<string, int>(expected, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return @string.Length >= expected;
            },
            (@string, _, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than or equal to {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<string, TAnd, TOr> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<string, int>(expected, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return @string.Length < expected;
            },
            (@string, _, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<string, TAnd, TOr> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<string, int>(expected, (@string, _, _, self) =>
            {
                if (@string is null)
                {
                    self.OverriddenMessage = "Actual string is null";
                    return false;
                }
                
                return @string.Length <= expected;
            },
            (@string, _, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than or equal to {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }
}