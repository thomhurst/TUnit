﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Equality;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class DisableReflectionScannerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider
            .WithComparer(new PreventCompilationTriggerOnEveryKeystrokeComparer());

        context.RegisterSourceOutput(provider, (sourceProductionContext, _) => GenerateCode(sourceProductionContext));
    }

    private void GenerateCode(SourceProductionContext context)
    {
        var sourceBuilder = new CodeWriter();

        sourceBuilder.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(DisableReflectionScannerGenerator).Assembly.GetName().Version}\")]");
        using (sourceBuilder.BeginBlock("file static class DisableReflectionScanner_" + Guid.NewGuid().ToString("N")))
        {
            sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (sourceBuilder.BeginBlock("public static void Initialize()"))
            {
                sourceBuilder.AppendLine("global::TUnit.Core.SourceRegistrar.IsEnabled = true;");
            }
        }

        context.AddSource("DisableReflectionScanner.g.cs", sourceBuilder.ToString());
    }

    private static bool IsSystemAssembly(IAssemblySymbol assemblySymbol)
    {
        // Check for well-known public key tokens of system assemblies
        var publicKeyToken = assemblySymbol.Identity.PublicKeyToken;

        if (publicKeyToken == null)
        {
            return false;
        }

        return publicKeyToken.SequenceEqual(new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 }) // .NET Framework
            || publicKeyToken.SequenceEqual(new byte[] { 0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e }) // .NET Core
            || publicKeyToken.SequenceEqual(new byte[] { 0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51 }) // System.Private
            || publicKeyToken.SequenceEqual(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }); // mscorlib
    }

    private static string GetAssemblyFullName(IAssemblySymbol assemblySymbol)
    {
        var identity = assemblySymbol.Identity;
        return $"{identity.Name}, Version={identity.Version}, Culture={(string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName)}, PublicKeyToken={(identity.PublicKeyToken.Length > 0 ? BitConverter.ToString(identity.PublicKeyToken.ToArray()).Replace("-", "").ToLowerInvariant() : "null")}";
    }

    private class AssemblyComparer : IEqualityComparer<ImmutableArray<MetadataReference>>
    {
        public bool Equals(ImmutableArray<MetadataReference> x, ImmutableArray<MetadataReference> y)
        {
            return GetAssemblyNamesString(x).Equals(GetAssemblyNamesString(y));
        }

        public int GetHashCode(ImmutableArray<MetadataReference> obj)
        {
            return GetAssemblyNamesString(obj).GetHashCode();
        }

        private static string GetAssemblyNamesString(ImmutableArray<MetadataReference> metadataReferences)
        {
            return string.Join("|", metadataReferences.Select(x => x.Display));
        }
    }
}
