namespace TUnit.Mock.Setup;

/// <summary>
/// Stores info about an event to auto-raise when a method setup is matched.
/// </summary>
public sealed record EventRaiseInfo(string EventName, object? Args);
