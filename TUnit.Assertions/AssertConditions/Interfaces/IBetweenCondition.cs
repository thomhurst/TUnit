namespace TUnit.Assertions.AssertConditions.Interfaces;

/// <summary>
/// Interface for between assertion conditions that support inclusive/exclusive bounds
/// </summary>
public interface IBetweenCondition
{
    void Inclusive();
    void Exclusive();
}