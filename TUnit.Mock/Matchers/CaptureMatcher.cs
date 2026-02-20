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

        // null case for reference types or nullable value types
        _capture.Add(default);
        return true;
    }

    public string Describe() => "Capture<" + typeof(T).Name + ">";
}
