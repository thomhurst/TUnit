namespace TUnit.Mock;

/// <summary>
/// Specifies the expected number of calls to a mocked member during verification.
/// </summary>
public readonly struct Times : IEquatable<Times>
{
    private readonly int _min;
    private readonly int _max;

    private Times(int min, int max) { _min = min; _max = max; }

    /// <summary>Expects the member to be called exactly once.</summary>
    public static Times Once => new(1, 1);

    /// <summary>Expects the member to never be called.</summary>
    public static Times Never => new(0, 0);

    /// <summary>Expects the member to be called one or more times.</summary>
    public static Times AtLeastOnce => AtLeast(1);

    /// <summary>Expects the member to be called exactly <paramref name="n"/> times.</summary>
    /// <param name="n">The exact number of expected calls.</param>
    public static Times Exactly(int n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "Value must be non-negative.");
        return new(n, n);
    }

    /// <summary>Expects the member to be called at least <paramref name="n"/> times.</summary>
    /// <param name="n">The minimum number of expected calls.</param>
    public static Times AtLeast(int n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "Value must be non-negative.");
        return new(n, int.MaxValue);
    }

    /// <summary>Expects the member to be called at most <paramref name="n"/> times.</summary>
    /// <param name="n">The maximum number of expected calls.</param>
    public static Times AtMost(int n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), n, "Value must be non-negative.");
        return new(0, n);
    }

    /// <summary>Expects the member to be called between <paramref name="min"/> and <paramref name="max"/> times (inclusive).</summary>
    /// <param name="min">The minimum number of expected calls.</param>
    /// <param name="max">The maximum number of expected calls.</param>
    public static Times Between(int min, int max)
    {
        if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, "Value must be non-negative.");
        if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, "max must be >= min.");
        return new(min, max);
    }

    // Internal method used by verification engine
    internal bool Matches(int actualCount) => actualCount >= _min && actualCount <= _max;

    // Returns true only when the constraint requires exactly zero calls (i.e. Never)
    internal bool RequiresZeroCalls => _min == 0 && _max == 0;

    // Returns true when the constraint allows zero calls (min == 0), covering AtMost and Between(0, N)
    internal bool AllowsZeroCalls => _min == 0;

    /// <inheritdoc />
    public override string ToString() => (_min, _max) switch
    {
        (0, 0) => "never",
        (1, 1) => "exactly once",
        var (min, max) when min == max => $"exactly {min} times",
        (_, int.MaxValue) => $"at least {_min} times",
        (0, _) => $"at most {_max} times",
        _ => $"between {_min} and {_max} times"
    };

    /// <inheritdoc />
    public bool Equals(Times other) => _min == other._min && _max == other._max;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Times t && Equals(t);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((_min * 397) ^ _max);

    /// <summary>Determines whether two <see cref="Times"/> values are equal.</summary>
    public static bool operator ==(Times left, Times right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Times"/> values are not equal.</summary>
    public static bool operator !=(Times left, Times right) => !left.Equals(right);
}
