using System;

namespace TUnit.Mock.SourceGenerator.Models;

internal sealed record MockEventModel : IEquatable<MockEventModel>
{
    public string Name { get; init; } = "";
    public string EventHandlerType { get; init; } = "";
    public string EventArgsType { get; init; } = "";
    public string? ExplicitInterfaceName { get; init; }

    public bool Equals(MockEventModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && EventHandlerType == other.EventHandlerType
            && EventArgsType == other.EventArgsType
            && ExplicitInterfaceName == other.ExplicitInterfaceName;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + EventHandlerType.GetHashCode();
            return hash;
        }
    }
}
