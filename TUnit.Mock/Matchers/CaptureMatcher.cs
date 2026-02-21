using TUnit.Mock.Arguments;

namespace TUnit.Mock.Matchers;

/// <summary>
/// An argument matcher that always matches and captures the argument value
/// into an <see cref="ArgCapture{T}"/> for later inspection.
/// </summary>
internal sealed class CaptureMatcher<T> : IArgumentMatcher<T>
{
    private readonly ArgCapture<T> _capture;

    public CaptureMatcher(ArgCapture<T> capture)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
    }

    public bool Matches(T? value)
    {
        _capture.Add(value);
        return true;
    }

    public bool Matches(object? value)
    {
        if (value is T typed)
        {
            return Matches(typed);
        }

        if (value is null)
        {
            // null is a valid value for reference types and nullable value types
            _capture.Add(default);
            return true;
        }

        // Type mismatch — do not capture a fabricated default
        return false;
    }

    public string Describe() => "Capture<" + typeof(T).Name + ">";
}
