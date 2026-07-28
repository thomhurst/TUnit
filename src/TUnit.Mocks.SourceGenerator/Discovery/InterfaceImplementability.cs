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
    /// implementer to provide is one the generated impl could declare. Members with a default
    /// implementation are not the implementer's problem and are ignored.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="MemberDiscovery.IsMemberAccessible"/>, the same rule that decides
    /// which members reach the model: a required member that discovery drops is exactly a member
    /// the impl will fail to implement. In particular <c>protected</c> and
    /// <c>protected internal</c> interface members stay implementable — a class in any assembly
    /// can satisfy them through explicit interface implementation — so only <c>internal</c> and
    /// <c>private protected</c> members from another assembly (without InternalsVisibleTo), or a
    /// member whose signature mentions an inaccessible type, make an interface unmockable.
    /// </remarks>
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

            if (!MemberDiscovery.IsMemberAccessible(member, compilation.Assembly))
            {
                return false;
            }
        }

        return true;
    }
}
