namespace TUnit.Assertions.AssertConditions.Interfaces;

/// <summary>
/// Interface for date assertion conditions that support tolerance
/// </summary>
public interface IDateToleranceCondition
{
    void SetTolerance(int toleranceDays);
}