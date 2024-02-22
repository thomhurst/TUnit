using System.Reflection;

namespace TUnit.Core;

internal record CachedAssemblyInformation
{
    public CachedAssemblyInformation(Assembly assembly)
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

        Methods = Types.SelectMany(x => x.GetMethods()).ToArray();
    }

    public Assembly Assembly { get; }

    public Type[] Types { get; }

    public MethodInfo[] Methods { get; }

    public virtual bool Equals(CachedAssemblyInformation? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Assembly.Equals(other.Assembly);
    }
    
    public override int GetHashCode()
    {
        return Assembly.GetHashCode();
    }
}