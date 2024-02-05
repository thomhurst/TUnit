using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class StringLength<TAnd, TOr> : Connector<string?, TAnd, TOr>
    where TAnd : And<string?, TAnd, TOr>, IAnd<TAnd, string?, TAnd, TOr>
    where TOr : Or<string?, TAnd, TOr>, IOr<TOr, string?, TAnd, TOr>
{
    protected AssertionBuilder<string?> AssertionBuilder { get; }

    public StringLength(AssertionBuilder<string?> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<string?, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<string?, TAnd, TOr> EqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {expected}")
        );
    }

    public BaseAssertCondition<string?, TAnd, TOr> IsEmpty =>
        Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder, 0, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == 0;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {0}")
        );

    public BaseAssertCondition<string?, TAnd, TOr> IsNotEmpty =>
        Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder, default, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length > 0;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to empty"
        ));


    public BaseAssertCondition<string?, TAnd, TOr> GreaterThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(
            AssertionBuilder,
            expected,
            (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length > expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than {expected}")
        );
    }

    public BaseAssertCondition<string?, TAnd, TOr> GreaterThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length >= expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than or equal to {expected}")
        );
    }

    public BaseAssertCondition<string?, TAnd, TOr> LessThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length < expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than {expected}")
        );
    }

    public BaseAssertCondition<string?, TAnd, TOr> LessThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int, TAnd, TOr>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length <= expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than or equal to {expected}")
        );
    }
}