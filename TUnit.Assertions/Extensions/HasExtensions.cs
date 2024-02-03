using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class HasExtensions
{
    public static EnumerableCount<T, TInner> Count<T, TInner>(this Is<T> @is) where T : IEnumerable<TInner>
    {
        return new EnumerableCount<T, TInner>(@is.AssertionBuilder);
    }
    
    public static StringLength Length(this Is<string> @is)
    {
        return new StringLength(@is.AssertionBuilder);
    }
}

public class EnumerableCount<T, TInner> where T : IEnumerable<TInner> 
{
    protected AssertionBuilder<T> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<T> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<T, int> EqualTo()
    {
        return new EnumerableCountEqualToAssertCondition<T, TInner>(@AssertionBuilder, 0);
    }
}

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
        });
    }
    
    public AssertCondition<string, int> GreaterThan(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
        {
            ArgumentNullException.ThrowIfNull(s);
            return s.Length > i;
        });
    }
    
    public AssertCondition<string, int> GreaterThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
        {
            ArgumentNullException.ThrowIfNull(s);
            return s.Length >= i;
        });
    }
    
    public AssertCondition<string, int> LessThan(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
        {
            ArgumentNullException.ThrowIfNull(s);
            return s.Length < i;
        });
    }
    
    public AssertCondition<string, int> LessThanOrEqualTo(int expected)
    {
        return new DelegateAssertCondition<string, int>(AssertionBuilder, expected, (s, i, arg3) =>
        {
            ArgumentNullException.ThrowIfNull(s);
            return s.Length <= i;
        });
    }
}