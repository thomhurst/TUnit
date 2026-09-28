using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Equality;

/// <summary>
/// Treats two compilations as equal when they would produce the same reference-derived output:
/// same language, assembly name and metadata references. Syntax-only edits (ordinary keystrokes)
/// keep the same <see cref="MetadataReference"/> instances, so they compare equal and the
/// downstream reference walk is skipped. Adding, removing or swapping a reference does not.
/// </summary>
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

        return x.Language == y.Language
               && x.AssemblyName == y.AssemblyName
               && ReferencesEqual(x.References, y.References);
    }

    public int GetHashCode(Compilation obj)
    {
        unchecked
        {
            return (obj.Language.GetHashCode() * 397) ^ (obj.AssemblyName != null ? obj.AssemblyName.GetHashCode() : 0);
        }
    }

    private static bool ReferencesEqual(IEnumerable<MetadataReference> x, IEnumerable<MetadataReference> y)
    {
        using var xEnumerator = x.GetEnumerator();
        using var yEnumerator = y.GetEnumerator();

        while (true)
        {
            var xHasNext = xEnumerator.MoveNext();
            var yHasNext = yEnumerator.MoveNext();

            if (xHasNext != yHasNext)
            {
                return false;
            }

            if (!xHasNext)
            {
                return true;
            }

            if (!ReferenceEqual(xEnumerator.Current, yEnumerator.Current))
            {
                return false;
            }
        }
    }

    private static bool ReferenceEqual(MetadataReference x, MetadataReference y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        // In the IDE a project reference is a CompilationReference that is recreated whenever the
        // referenced project is edited. Comparing it by identity would rerun the reference walk on
        // every keystroke in that project, so compare the referenced assembly name instead.
        if (x is CompilationReference xCompilation && y is CompilationReference yCompilation)
        {
            return xCompilation.Compilation.AssemblyName == yCompilation.Compilation.AssemblyName
                   && xCompilation.Properties.Equals(yCompilation.Properties);
        }

        // Hosts reuse PortableExecutableReference instances for unchanged files, so a different
        // instance means the reference was added, removed or rebuilt.
        return false;
    }
}
