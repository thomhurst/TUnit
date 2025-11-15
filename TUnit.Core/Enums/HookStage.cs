namespace TUnit.Core.Enums;

/// <summary>
/// Defines the stage at which event receivers execute relative to instance-level hooks.
/// </summary>
public enum HookStage
{
    /// <summary>
    /// Event receiver runs early, before instance-level [Before(Test)] or [After(Test)] hooks.
    /// </summary>
    Early = 0,
    
    /// <summary>
    /// Event receiver runs late, after instance-level [Before(Test)] or [After(Test)] hooks (default behavior).
    /// </summary>
    Late = 1
}
