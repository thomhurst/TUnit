namespace TUnit.Engine;

internal class ClassWalker
{
    public IEnumerable<Type> GetSelfAndBaseTypes(Type? type)
    {
        while (type != null)
        {
            yield return type;
            type = type.BaseType;
        }
    }
}