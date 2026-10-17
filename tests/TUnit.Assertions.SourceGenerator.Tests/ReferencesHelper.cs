using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Tests;

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
                MetadataReference.CreateFromFile(typeof(Polyfill).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                // Not loaded by a plain test run, but loaded by IDE/`dotnet test` server-mode hosts, so it must
                // be explicit or HttpStatusCode resolves differently depending on how the tests are launched.
                MetadataReference.CreateFromFile(typeof(System.Net.HttpStatusCode).Assembly.Location),
            ])
            .ToList();
}
