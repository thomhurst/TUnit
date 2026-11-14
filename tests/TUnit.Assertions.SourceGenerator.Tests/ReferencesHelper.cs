using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file")]
internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        TestHostReferences.CreateReferences(
            typeof(Attribute).Assembly,
            typeof(Assert).Assembly,
            typeof(Polyfill).Assembly,
            typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute).Assembly,
            typeof(Uri).Assembly);

    // The test assembly contains the fixtures too, and its copy would be found first. It is an .exe on
    // .NET Framework, so compare the name without its extension.
    public static IEnumerable<MetadataReference> WithoutTestAssembly() =>
        References.Where(reference => !string.Equals(
            Path.GetFileNameWithoutExtension(reference.FilePath),
            "TUnit.Assertions.SourceGenerator.Tests",
            StringComparison.OrdinalIgnoreCase));
}
