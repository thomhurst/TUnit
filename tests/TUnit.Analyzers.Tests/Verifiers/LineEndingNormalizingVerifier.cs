using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Analyzers.Tests.Verifiers;

/// <summary>
/// A custom verifier that normalizes line endings to LF before comparison to support cross-platform testing.
/// This prevents tests from failing due to differences between Windows (CRLF) and Unix (LF) line endings.
/// By normalizing to LF (the universal standard), tests pass consistently on all platforms.
/// </summary>
public class LineEndingNormalizingVerifier : IVerifier
{
    private readonly DefaultVerifier _defaultVerifier = new();

    public void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        _defaultVerifier.Empty(collectionName, collection);
    }

    public void Equal<T>(T expected, T actual, string? message = null)
    {
        // Normalize line endings for string comparisons
        if (expected is string expectedString && actual is string actualString)
        {
            var normalizedExpected = NormalizeLineEndings(expectedString);
            var normalizedActual = NormalizeLineEndings(actualString);
            _defaultVerifier.Equal(normalizedExpected, normalizedActual, message);
        }
        else
        {
            _defaultVerifier.Equal(expected, actual, message);
        }
    }

    public void True(bool assert, string? message = null)
    {
        _defaultVerifier.True(assert, message);
    }

    public void False(bool assert, string? message = null)
    {
        _defaultVerifier.False(assert, message);
    }

    [DoesNotReturn]
    public void Fail(string? message = null)
    {
        _defaultVerifier.Fail(message);
    }

    public void LanguageIsSupported(string language)
    {
        _defaultVerifier.LanguageIsSupported(language);
    }

    public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
    {
        _defaultVerifier.NotEmpty(collectionName, collection);
    }

    public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
    {
        // Normalize line endings for string sequence comparisons
        if (typeof(T) == typeof(string))
        {
            var normalizedExpected = expected.Cast<string>().Select(NormalizeLineEndings).Cast<T>();
            var normalizedActual = actual.Cast<string>().Select(NormalizeLineEndings).Cast<T>();
            _defaultVerifier.SequenceEqual(normalizedExpected, normalizedActual, equalityComparer, message);
        }
        else
        {
            _defaultVerifier.SequenceEqual(expected, actual, equalityComparer, message);
        }
    }

    public IVerifier PushContext(string context)
    {
        // Create a new verifier that wraps the result of PushContext on the default verifier
        return new LineEndingNormalizingVerifierWithContext(_defaultVerifier.PushContext(context));
    }

    private static string NormalizeLineEndings(string value)
    {
        // Normalize all line endings to LF (Unix) for cross-platform consistent comparison
        // LF is the universal standard and prevents Windows/Linux test mismatches
        return value.Replace("\r\n", "\n");
    }

    /// <summary>
    /// Internal helper class to wrap a verifier with context
    /// </summary>
    private class LineEndingNormalizingVerifierWithContext : LineEndingNormalizingVerifier
    {
        private readonly IVerifier _wrappedVerifier;

        public LineEndingNormalizingVerifierWithContext(IVerifier wrappedVerifier)
        {
            _wrappedVerifier = wrappedVerifier;
        }
    }
}
