using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class StringLength
{
    private readonly IValueSource<string> _valueSource;
    protected AssertionBuilder<string> AssertionBuilder { get; }

    public StringLength(IValueSource<string> valueSource)
    {
        _valueSource = valueSource;
        AssertionBuilder = valueSource.AssertionBuilder.AppendExpression("HasLength");
    }

    public InvokableValueAssertionBuilder<string> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(expected, (actual, _, self) =>
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
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<string> Zero =>
        _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
                (@string, _, self) =>
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
            , []);

    public InvokableValueAssertionBuilder<string> Positive =>
        _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(default, (@string, _, self) =>
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
        ), []);


    public InvokableValueAssertionBuilder<string> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(expected,
            (@string, _, self) =>
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
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<string> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(expected, (@string, _, self) =>
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
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<string> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(expected, (@string, _, self) =>
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
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<string> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(expected, (@string, _, self) =>
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
            , [doNotPopulateThisValue]);
    }
}