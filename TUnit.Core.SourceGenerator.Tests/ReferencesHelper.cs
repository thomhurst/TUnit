using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Core.SourceGenerator.Tests;

[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file")]
internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat([
                // add your app/lib specifics, e.g.:
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location),
                MetadataReference.CreateFromFile("TUnit.TestProject.Library.dll"),
                MetadataReference.CreateFromFile("TUnit.Core.dll"),
                MetadataReference.CreateFromFile(typeof(Polyfill).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(UnconditionalSuppressMessageAttribute).Assembly.Location),
            ])
            .ToList();
}
