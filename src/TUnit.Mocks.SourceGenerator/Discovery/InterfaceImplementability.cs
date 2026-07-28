using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Decides whether the consuming compilation could declare a type that implements a given
/// interface at all. Since C# 8 an interface may declare non-public abstract members — RavenDB's
/// <c>ISessionBlittableJsonConverter.MissingProperties</c> is <c>internal</c> — and no type outside
/// the declaring assembly can satisfy those slots. Generating a mock for such an interface always
/// produces CS0535/CS0548/CS0551 on code the user never wrote, so it is skipped instead.
/// See issue #6491.
/// </summary>
internal static class InterfaceImplementability
{
    /// <summary>
    /// True when every abstract member the interface (and its base interfaces) requires an
    /// implementer to provide is accessible from <paramref name="compilation"/>'s assembly.
    /// Members with a default implementation are not the implementer's problem and are ignored.
    /// </summary>
    internal static bool CanBeImplemented(INamedTypeSymbol interfaceType, Compilation compilation)
    {
        if (interfaceType.TypeKind != TypeKind.Interface)
        {
            return true;
        }

        // AllInterfaces excludes the type itself, so check both.
        return AllRequiredMembersAccessible(interfaceType, compilation)
            && interfaceType.AllInterfaces.All(i => AllRequiredMembersAccessible(i, compilation));
    }

    private static bool AllRequiredMembersAccessible(INamedTypeSymbol interfaceType, Compilation compilation)
    {
        foreach (var member in interfaceType.GetMembers())
        {
            if (!member.IsAbstract)
            {
                continue;
            }

            // Accessors are checked through their associated property/event, which carries the
            // accessibility that actually gates implementation.
            if (member is IMethodSymbol { AssociatedSymbol: not null })
            {
                continue;
            }

            if (!compilation.IsSymbolAccessibleWithin(member, compilation.Assembly))
            {
                return false;
            }
        }

        return true;
    }
}
