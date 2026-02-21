using System.ComponentModel;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Verification;

/// <summary>
/// Verifies recorded property calls (getter and setter).
/// Created by the generated verify surface. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class PropertyVerificationBuilder<T> : IPropertyVerification where T : class
{
    private readonly MockEngine<T> _engine;
    private readonly int _getterMemberId;
    private readonly int _setterMemberId;
    private readonly string _propertyName;

    public PropertyVerificationBuilder(MockEngine<T> engine, int getterMemberId, int setterMemberId, string propertyName)
    {
        _engine = engine;
        _getterMemberId = getterMemberId;
        _setterMemberId = setterMemberId;
        _propertyName = propertyName;
    }

    /// <inheritdoc />
    public void GetWasCalled(Times times)
    {
        var calls = _engine.GetCallsFor(_getterMemberId);
        if (!times.Matches(calls.Count))
        {
            var expectedCall = $"get_{_propertyName}";
            var actualCallDescriptions = calls.Select(c => c.FormatCall()).ToList();
            throw new MockVerificationException(expectedCall, times, calls.Count, actualCallDescriptions);
        }
    }

    /// <inheritdoc />
    public void GetWasCalled() => GetWasCalled(Times.AtLeastOnce);

    /// <inheritdoc />
    public void WasSetTo(object? value)
    {
        var calls = _engine.GetCallsFor(_setterMemberId);
        var matcher = new ExactValueMatcher<object?>(value);

        var matchingCount = 0;
        foreach (var call in calls)
        {
            if (call.Arguments.Length == 1 && matcher.Matches(call.Arguments[0]))
            {
                matchingCount++;
            }
        }

        if (matchingCount == 0)
        {
            var expectedCall = $"set_{_propertyName}({value ?? "null"})";
            var actualCallDescriptions = calls.Select(c => c.FormatCall()).ToList();
            throw new MockVerificationException(expectedCall, Times.AtLeastOnce, matchingCount, actualCallDescriptions);
        }
    }

    /// <inheritdoc />
    public void SetWasCalled(Times times)
    {
        var calls = _engine.GetCallsFor(_setterMemberId);
        if (!times.Matches(calls.Count))
        {
            var expectedCall = $"set_{_propertyName}";
            var actualCallDescriptions = calls.Select(c => c.FormatCall()).ToList();
            throw new MockVerificationException(expectedCall, times, calls.Count, actualCallDescriptions);
        }
    }

    /// <inheritdoc />
    public void SetWasCalled() => SetWasCalled(Times.AtLeastOnce);

    /// Helper matcher for exact value comparison
    private sealed class ExactValueMatcher<TValue> : IArgumentMatcher
    {
        private readonly TValue _expected;

        public ExactValueMatcher(TValue expected) => _expected = expected;

        public bool Matches(object? argument) =>
            argument is TValue typed ? EqualityComparer<TValue>.Default.Equals(typed, _expected) : argument is null && _expected is null;

        public string Describe() => _expected?.ToString() ?? "null";
    }
}
