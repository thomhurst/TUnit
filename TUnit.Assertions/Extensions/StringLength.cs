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
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(s);
                return s.Length == i;
            },
            (s, i, arg3) => $"{s} was {s?.Length} characters long but expected to be equal to {i}");
    }
    
    public AssertCondition<string, int> Empty =>
        new DelegateAssertCondition<string, int>(AssertionBuilder, 0, (s, i, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(s);
                return s.Length == i;
            },
            (s, i, arg3) => $"{s} was {s?.Length} characters long but expected to be equal to {i}");

    public AssertCondition<string, int> GreaterThan(int expected)
    {
        return new DelegateAssertCondition<string, int>(
            AssertionBuilder, 
            expected, 
            (s, i, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(s);
                return s.Length > i;
            },
            (s, i, arg3) => $"{s} was {s?.Length} characters long but expected to be greater than {i}");
    }
    
    public AssertCondition<string, int> GreaterThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(s);
                return s.Length >= i;
            },
            (s, i, arg3) => $"{s} was {s?.Length} characters long but expected to be greater than or equal to {i}");
    }
    
    public AssertCondition<string, int> LessThan(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(s);
                return s.Length < i;
            },
            (s, i, arg3) => $"{s} was {s?.Length} characters long but expected to be less than {i}");
    }
    
    public AssertCondition<string, int> LessThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
            {
                ArgumentNullException.ThrowIfNull(s);
                return s.Length <= i;
            },
            (s, i, arg3) => $"{s} was {s?.Length} characters long but expected to be less than or equal to {i}");
    }
}