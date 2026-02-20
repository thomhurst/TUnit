namespace TUnit.Mock.Verification;

/// <summary>
/// Provides verification methods for asserting property getter and setter calls.
/// </summary>
public interface IPropertyVerification
{
    /// <summary>Verifies the property getter was called the specified number of times.</summary>
    void GetWasCalled(Times times);

    /// <summary>Verifies the property getter was called at least once.</summary>
    void GetWasCalled();

    /// <summary>Verifies the property setter was called with the specified value.</summary>
    void WasSetTo(object? value);

    /// <summary>Verifies the property setter was called the specified number of times.</summary>
    void SetWasCalled(Times times);

    /// <summary>Verifies the property setter was called at least once.</summary>
    void SetWasCalled();
}
