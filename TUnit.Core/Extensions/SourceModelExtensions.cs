namespace TUnit.Core.Extensions;

internal static class SourceModelExtensions
{
    public static bool HasAttribute<T>(this MemberMetadata member)
    {
        return member.Attributes.OfType<T>().Any();
    }
}
