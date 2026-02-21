namespace TUnit.Mock;

/// <summary>
/// Marker interface for generated mock setup surfaces.
/// Extension methods on this interface provide strongly-typed setup members.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface IMockSetup<T> where T : class { }

/// <summary>
/// Marker interface for generated mock verify surfaces.
/// Extension methods on this interface provide strongly-typed verify members.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface IMockVerify<T> where T : class { }

/// <summary>
/// Marker interface for generated mock raise surfaces.
/// Extension methods on this interface provide strongly-typed raise members.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface IMockRaise<T> where T : class { }
