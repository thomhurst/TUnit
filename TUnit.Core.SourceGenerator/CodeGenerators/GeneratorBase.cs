using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

public abstract class GeneratorBase : IIncrementalGenerator
{
    public abstract void Initialize(IncrementalGeneratorInitializationContext context);

    protected string ToFileNameString(string filename)
    {
        var sanitizedFilename = filename
            .Replace(':', '_')
            .Replace('.', '_')
            .Replace('-', '_');

        return $"{sanitizedFilename}_{Guid.NewGuid():N}";
    }
}