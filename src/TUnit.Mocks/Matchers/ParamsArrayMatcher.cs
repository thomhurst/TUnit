using System.ComponentModel;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Matchers;

/// <summary>
/// Composite matcher for <c>params T[]</c> parameters: matches the packed argument array
/// element-by-element against the per-element matchers supplied at the setup call site.
/// Occupies a single top-level matcher slot so setup arity stays equal to the declared
/// parameter count. Public for generated code access. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ParamsArrayMatcher : IArgumentMatcher, ICapturingMatcher
{
    private readonly IArgumentMatcher[] _elementMatchers;

    /// <summary>Creates a matcher from per-element matchers. Public for generated code access.</summary>
    public ParamsArrayMatcher(IArgumentMatcher[] elementMatchers)
    {
        _elementMatchers = elementMatchers ?? throw new ArgumentNullException(nameof(elementMatchers));
    }

    public bool Matches(object? value)
    {
        // Reference-element params (object[], string[], ...) are object?[]-compatible via array
        // covariance — index directly, no per-element GetValue call.
        if (value is object?[] referenceArray)
        {
            if (referenceArray.Length != _elementMatchers.Length)
            {
                return false;
            }

            for (var i = 0; i < _elementMatchers.Length; i++)
            {
                if (!_elementMatchers[i].Matches(referenceArray[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Value-element params pack as the concrete array type (e.g. int[] for params int[]),
        // which is not object?[] — match via System.Array.
        if (value is not Array array || array.Length != _elementMatchers.Length)
        {
            return false;
        }

        for (var i = 0; i < _elementMatchers.Length; i++)
        {
            if (!_elementMatchers[i].Matches(array.GetValue(i)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Forwards deferred capture to each element matcher so per-element
    /// <see cref="Arg{T}.Values"/>/<see cref="Arg{T}.Latest"/> still work.
    /// <see cref="Setup.MethodSetup"/> only walks top-level matchers.
    /// </summary>
    void ICapturingMatcher.ApplyCapture(object? value)
    {
        // Mirror the index strategy of Matches: covariant fast-path for reference-element arrays,
        // System.Array fallback for value-element arrays.
        if (value is object?[] referenceArray)
        {
            if (referenceArray.Length != _elementMatchers.Length)
            {
                return;
            }

            for (var i = 0; i < _elementMatchers.Length; i++)
            {
                if (_elementMatchers[i] is ICapturingMatcher capturing)
                {
                    capturing.ApplyCapture(referenceArray[i]);
                }
            }

            return;
        }

        if (value is not Array array || array.Length != _elementMatchers.Length)
        {
            return;
        }

        for (var i = 0; i < _elementMatchers.Length; i++)
        {
            if (_elementMatchers[i] is ICapturingMatcher capturing)
            {
                capturing.ApplyCapture(array.GetValue(i));
            }
        }
    }

    public string Describe() =>
        $"[{string.Join(", ", _elementMatchers.Select(m => m.Describe()))}] ({_elementMatchers.Length} element(s))";
}
