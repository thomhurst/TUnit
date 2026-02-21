using System;

namespace TUnit.Mocks.SourceGenerator.Models;

internal sealed record MockEventModel : IEquatable<MockEventModel>
{
    public string Name { get; init; } = "";
    public string EventHandlerType { get; init; } = "";

    /// <summary>
    /// The parameter list for the generated Raise method signature.
    /// E.g., "string args" for EventHandler&lt;string&gt;, or "string arg1, int arg2" for Action&lt;string, int&gt;.
    /// </summary>
    public string RaiseParameters { get; init; } = "";

    /// <summary>
    /// The argument expression for invoking the backing delegate.
    /// E.g., "this, args" for EventHandler&lt;string&gt;, or "arg1, arg2" for Action&lt;string, int&gt;.
    /// </summary>
    public string InvokeArgs { get; init; } = "";

    /// <summary>
    /// Kept for backward compat with MockRaiseBuilder extension method parameter.
    /// Same as RaiseParameters for simple cases.
    /// </summary>
    public string EventArgsType { get; init; } = "";

    public string? ExplicitInterfaceName { get; init; }

    /// <summary>
    /// Structured representation of raise parameters. Use this instead of parsing RaiseParameters
    /// by comma — which breaks for generic types like Func&lt;int, string&gt;.
    /// </summary>
    public EquatableArray<MockParameterModel> RaiseParameterList { get; init; } = EquatableArray<MockParameterModel>.Empty;

    public bool Equals(MockEventModel? other)
    {
        if (other is null) return false;
        return Name == other.Name
            && EventHandlerType == other.EventHandlerType
            && RaiseParameters == other.RaiseParameters
            && InvokeArgs == other.InvokeArgs
            && EventArgsType == other.EventArgsType
            && ExplicitInterfaceName == other.ExplicitInterfaceName
            && RaiseParameterList == other.RaiseParameterList;
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
