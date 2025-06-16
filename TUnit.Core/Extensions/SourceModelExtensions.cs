namespace TUnit.Core.Extensions;

internal static class SourceModelExtensions
{
    public static bool HasAttribute<T>(this TestMember member)
    {
        return member.Attributes.OfType<T>().Any();
    }
}
