using System;

namespace TUnit.Assertions.AssertConditions.Interfaces;

/// <summary>
/// Interface for time assertion conditions that support tolerance
/// </summary>
public interface ITimeToleranceCondition
{
    void SetTolerance(TimeSpan tolerance);
}