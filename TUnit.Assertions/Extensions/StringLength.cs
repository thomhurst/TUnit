using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class StringLength : Connector<string>
{
    protected AssertionBuilder<string> AssertionBuilder { get; }

    public StringLength(AssertionBuilder<string> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<string>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<string> EqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {expected}")
        );
    }

    public BaseAssertCondition<string> IsEmpty =>
        Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, 0, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == 0;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {0}")
        );

    public BaseAssertCondition<string> IsNotEmpty =>
        Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, default, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length > 0;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to empty"
        ));


    public BaseAssertCondition<string> GreaterThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(
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

    public BaseAssertCondition<string> GreaterThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length >= expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than or equal to {expected}")
        );
    }

    public BaseAssertCondition<string> LessThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length < expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than {expected}")
        );
    }

    public BaseAssertCondition<string> LessThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length <= expected;
            },
            (@string, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than or equal to {expected}")
        );
    }
}