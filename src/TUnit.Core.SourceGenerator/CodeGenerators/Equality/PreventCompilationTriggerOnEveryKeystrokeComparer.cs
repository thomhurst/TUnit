using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Equality;

public class PreventCompilationTriggerOnEveryKeystrokeComparer : IEqualityComparer<Compilation>
{
    public bool Equals(Compilation? x, Compilation? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return false;
        }

        if (y is null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Language == y.Language && x.AssemblyName == y.AssemblyName;
    }

    public int GetHashCode(Compilation obj)
    {
        unchecked
        {
            return (obj.Language.GetHashCode() * 397) ^ (obj.AssemblyName != null ? obj.AssemblyName.GetHashCode() : 0);
        }
    }
}
