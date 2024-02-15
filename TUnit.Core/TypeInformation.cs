using System.Reflection;

namespace TUnit.Core;

internal record TypeInformation
{
    public TypeInformation(Assembly assembly)
    {
        Assembly = assembly;
        
        try
        {
            Types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            Types = e.Types.OfType<Type>().ToArray();
        }
    }

    public Type[] Types { get; }
    public Assembly Assembly { get; init; }

    public void Deconstruct(out Assembly Assembly)
    {
        Assembly = this.Assembly;
    }
}