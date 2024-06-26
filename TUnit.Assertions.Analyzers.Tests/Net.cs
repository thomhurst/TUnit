using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Assertions.Analyzers.Tests;

internal static class Net
{
    private static readonly Lazy<ReferenceAssemblies> LazyNet80 = new(() =>
        new ReferenceAssemblies(
                "net8.0",
                new PackageIdentity(
                    "Microsoft.NETCore.App.Ref",
                    "8.0.1"),
                Path.Combine("ref", "net8.0"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.Extensions.Logging", "8.0.0")))
    );

    public static ReferenceAssemblies Net80 => LazyNet80.Value;
}