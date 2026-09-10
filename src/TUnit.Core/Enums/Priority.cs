namespace TUnit.Core.Enums;

/// <summary>
/// Defines the priority levels for test execution
/// </summary>
public enum Priority
{
    /// <summary>
    /// Lowest priority - runs last
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Below normal priority
    /// </summary>
    BelowNormal = 1,
    
    /// <summary>
    /// Normal priority (default)
    /// </summary>
    Normal = 2,
    
    /// <summary>
    /// Above normal priority
    /// </summary>
    AboveNormal = 3,
    
    /// <summary>
    /// High priority
    /// </summary>
    High = 4,
    
    /// <summary>
    /// Critical priority - runs first
    /// </summary>
    Critical = 5
}