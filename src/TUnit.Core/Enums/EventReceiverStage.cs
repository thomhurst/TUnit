namespace TUnit.Core.Enums;

/// <summary>
/// Defines the execution stage for event receivers relative to instance-level hooks
/// </summary>
public enum EventReceiverStage
{
    /// <summary>
    /// Execute before instance-level hooks ([Before(Test)], [After(Test)])
    /// </summary>
    Early = 0,

    /// <summary>
    /// Execute after instance-level hooks (default behavior for backward compatibility)
    /// </summary>
    Late = 1
}
