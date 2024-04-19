using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Tests;

public class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[]
            {
                // add your app/lib specifics, e.g.:                      
                MetadataReference.CreateFromFile(typeof(Core.TestAttribute).Assembly.Location),
            })
            .ToList();
}