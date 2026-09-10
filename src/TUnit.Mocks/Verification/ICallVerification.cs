namespace TUnit.Mocks.Verification;

/// <summary>
/// Provides verification methods for asserting how many times a mock member was called.
/// </summary>
public interface ICallVerification
{
    /// <summary>Verifies the member was called the specified number of times.</summary>
    void WasCalled(Times times);

    /// <summary>Verifies the member was called the specified number of times, with a custom failure message.</summary>
    void WasCalled(Times times, string? message);

    /// <summary>Verifies the member was never called. Shorthand for <c>WasCalled(Times.Never)</c>.</summary>
    void WasNeverCalled();

    /// <summary>Verifies the member was never called, with a custom failure message.</summary>
    void WasNeverCalled(string? message);

    /// <summary>Verifies the member was called at least once. Shorthand for <c>WasCalled(Times.AtLeastOnce)</c>.</summary>
    void WasCalled();

    /// <summary>Verifies the member was called at least once, with a custom failure message.</summary>
    void WasCalled(string? message);
}
