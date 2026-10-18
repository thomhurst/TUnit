using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file")]
internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        TestHostReferences.AssemblyPaths
            .Select(path => MetadataReference.CreateFromFile(path))
            .Concat([
                // add your app/lib specifics, e.g.:
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Polyfill).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
            ])
            .ToList();
}
