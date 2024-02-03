using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class StringLength 
{
    protected AssertionBuilder<string> AssertionBuilder { get; }

    public StringLength(AssertionBuilder<string> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<string, int> EqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == length;
            },
            (@string, length, arg3) => $"{@string} was {@string?.Length} characters long but expected to be equal to {length}");
    }
    
    public AssertCondition<string, int> Empty =>
        new DelegateAssertCondition<string, int>(AssertionBuilder, 0, (@string, length, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length == length;
            },
            (@string, length, arg3) => $"{@string} was {@string?.Length} characters long but expected to be equal to {length}");

    public AssertCondition<string, int> GreaterThan(int expected)
    {
        return new DelegateAssertCondition<string, int>(
            AssertionBuilder, 
            expected, 
            (@string, length, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length > length;
            },
            (@string, length, arg3) => $"{@string} was {@string?.Length} characters long but expected to be greater than {length}");
    }
    
    public AssertCondition<string, int> GreaterThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length >= length;
            },
            (@string, length, arg3) => $"{@string} was {@string?.Length} characters long but expected to be greater than or equal to {length}");
    }
    
    public AssertCondition<string, int> LessThan(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length < length;
            },
            (@string, length, arg3) => $"{@string} was {@string?.Length} characters long but expected to be less than {length}");
    }
    
    public AssertCondition<string, int> LessThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (@string, length, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(@string);
                return @string.Length <= length;
            },
            (@string, length, arg3) => $"{@string} was {@string?.Length} characters long but expected to be less than or equal to {length}");
    }
}