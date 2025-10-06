// The existing infrastructure has lots of extra types and overhead with "AssertionBuilders" and wrappers etc. Is it possible to simplify this to just have simpler assertion types that can chain together? 
// This is a first pass at that idea, to see if it can be done in a simpler way - Consider the following pseudo-code usage:

public class AssertionMetadata
{
    public DateTime _startTime { get; set; } = DateTime.MinValue;
    public DateTime _endTime { get; set; } = DateTime.MinValue;
    public TimeSpan _delegateDuration { get; set; } = TimeSpan.Zero;
}

public abstract class Assertion<TSource, TBuilder> where TBuilder : IAssertionBuilder
{
    private readonly List<Assertion> _assertions;

    protected Assertion(List<Assertion> assertions)
    {
        _assertions = assertions;
    }

    // Optional description/reason for the assertion
    public string? Because { get; set; }

    protected abstract Task Assert(TSource? actual, Exception? exception, AssertionMetadata metadata);

    public async Task Assert()
    {
        foreach (var assertion in _assertions)
        {
            try
            {
                if (assertion is IDelegateSource delegateSource)
                {
                    await delegateSource.InvokeDelegate();

                    // No exception
                    await Assert(default, null, new AssertionMetadata());
                }
                else if (assertion is IValueSource<TSource> valueSource)
                {
                    var value = await valueSource.GetValue();

                    await Assert(value, null, new AssertionMetadata());
                }
                else
                {
                    throw new InvalidOperationException("Unknown assertion type.");
                }
            }
            catch (System.Exception exception)
            {
                await Assert(default, exception, new AssertionMetadata());
            }
        }
    }

    // Awaiting an assertion runs it
    // For value types, can we return a TaskAwaiter<T> so that we can return the actual value directly?
    public TaskAwaiter<TSource> GetAwaiter()
    {
        return Assert().GetAwaiter();
    }

    // Chaining with the correct TBuilder type allows for the type system to enforce correct chaining and only show relevant assertions
    public TBuilder And => new AndAssertion<TSource, TBuilder>(_assertions) as TBuilder;
    
    // Chaining with the correct TBuilder type allows for the type system to enforce correct chaining and only show relevant assertions
    public TBuilder Or => new OrAssertion<TSource, TBuilder>(_assertions) as TBuilder;
}

public interface IValueSource<TValue> : IAssertionBuilder
{
    Task<TValue> GetValue();
}

public interface IDelegateSource : IAssertionBuilder
{
    Task InvokeDelegate();
}

public interface IValueDelegateSource<TValue> : IValueSource<TValue>, IDelegateSource
{
}

public interface IAssertionBuilder
{
}

// Chalenges include making sure that "And" and "Or" return the correct types, and that the correct assertions are available at each stage.
// This may require some advanced use of generics and interfaces to ensure type safety and correct chaining

// Some assertions may change and map to different values for fluent chaining - E.g. await Assertion.OfType<T>() should return an Assertion<T> type for further assertions on that type
// Some assertions may not change the type, but just add more constraints - E.g. await Assertion.NotNull() should return an Assertion<T> type for further assertions on that type

// Examine the existing API and see how it can be mapped to this new structure, making sure we maintain features, but try to simplify the overall design and usage, while keeping it flexible and extensible for future assertions and types