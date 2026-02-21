namespace TUnit.Mocks;

/// <summary>
/// Specifies how a mock behaves when an unconfigured member is invoked.
/// </summary>
public enum MockBehavior
{
    /// <summary>
    /// Unconfigured calls return smart defaults (default behavior).
    /// </summary>
    Loose = 0,

    /// <summary>
    /// Unconfigured calls throw <see cref="Exceptions.MockStrictBehaviorException"/>.
    /// </summary>
    Strict = 1
}
