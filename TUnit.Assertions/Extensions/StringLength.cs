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
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == length;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {length}")
        );
    }

    public BaseAssertCondition<string> IsEmpty =>
        Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, 0, (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == length;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be equal to {length}")
        );

    public BaseAssertCondition<string> IsNotEmpty =>
        Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, default, (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length > 0;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to empty"
        ));


    public BaseAssertCondition<string> GreaterThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(
            AssertionBuilder,
            expected,
            (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length > length;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than {length}")
        );
    }

    public BaseAssertCondition<string> GreaterThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length >= length;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be greater than or equal to {length}")
        );
    }

    public BaseAssertCondition<string> LessThan(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length < length;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than {length}")
        );
    }

    public BaseAssertCondition<string> LessThanOrEqualTo(int expected)
    {
        return Wrap(new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, _) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length <= length;
            },
            (@string, length, _) =>
                $"\"{@string}\" was {@string?.Length} characters long but expected to be less than or equal to {length}")
        );
    }
}