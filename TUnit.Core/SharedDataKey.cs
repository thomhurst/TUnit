namespace TUnit.Core;

/// <summary>
/// Represents a key for shared data.
/// </summary>
/// <param name="Key">The key.</param>
/// <param name="Type">The type of the data.</param>
public record SharedDataKey(string Key, Type Type);