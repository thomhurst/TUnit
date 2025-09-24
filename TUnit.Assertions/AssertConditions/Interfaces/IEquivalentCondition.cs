using System;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertConditions.Interfaces;

/// <summary>
/// Interface for equivalent assertion conditions that support ignoring members and types
/// </summary>
public interface IEquivalentCondition
{
    EquivalencyKind EquivalencyKind { get; set; }
    void IgnoringMember(string propertyName);
    void IgnoringType(Type type);
}