using System;

namespace TUnit.Mocks;

/// <summary>
/// Provides custom default values for unconfigured mock method return types.
/// Implement this interface to control what values are returned when no setup matches
/// and the mock is in loose mode.
/// </summary>
public interface IDefaultValueProvider
{
    /// <summary>
    /// Determines whether this provider can supply a default value for the given type.
    /// </summary>
    /// <param name="type">The return type of the unconfigured method.</param>
    /// <returns><c>true</c> if this provider can supply a value; otherwise <c>false</c>.</returns>
    bool CanProvide(Type type);

    /// <summary>
    /// Gets the default value for the given type.
    /// Only called when <see cref="CanProvide"/> returns <c>true</c>.
    /// </summary>
    /// <param name="type">The return type of the unconfigured method.</param>
    /// <returns>A default value, or <c>null</c>.</returns>
    object? GetDefaultValue(Type type);
}
