using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Tests.Shared;

namespace TUnit.Core.SourceGenerator.Tests;

[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file")]
internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        TestHostReferences.CreateReferences(
            typeof(Attribute).Assembly,
            typeof(Assert).Assembly,
            typeof(Polyfill).Assembly);
}
