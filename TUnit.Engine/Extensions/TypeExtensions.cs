namespace TUnit.Engine.Extensions;

internal static class TypeExtensions
{
    public static IEnumerable<Type> GetSelfAndBaseTypes(this Type type)
    {
        var nullableType = type;
        
        while (nullableType != null)
        {
            yield return nullableType;
            nullableType = nullableType.BaseType;
        }
    }
}