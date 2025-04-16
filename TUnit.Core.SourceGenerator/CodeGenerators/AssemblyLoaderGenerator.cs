﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class AssemblyLoaderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.CompilationProvider
            .Select((x, _) => x.GetUsedAssemblyReferences())
            .WithComparer(new AssemblyComparer())
            .Combine(context.CompilationProvider);
        
        context.RegisterSourceOutput(provider, (sourceProductionContext, source) => GenerateCode(sourceProductionContext, source.Right, source.Left));
    }

    private void GenerateCode(SourceProductionContext context, Compilation compilation, ImmutableArray<MetadataReference> metadataReferences)
    {
        var assemblyReferences = metadataReferences.Where(x => x.Properties.Kind == MetadataImageKind.Assembly);

        var assemblySymbols = assemblyReferences
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Where(x => !IsSystemAssembly(x))
            .ToArray();

        var types = assemblySymbols
            .Select(x => x.GlobalNamespace)
            .Select(GetFirstType)
            .OfType<INamedTypeSymbol>()
            .Where(x => x.TypeKind is TypeKind.Class or TypeKind.Struct)
            .ToArray();
        
        var sourceBuilder = new SourceCodeWriter();
        
        sourceBuilder.WriteLine("// <auto-generated/>");
        sourceBuilder.WriteLine("#pragma warning disable");
        
        if(!string.IsNullOrEmpty(compilation.Assembly.Name))
        {
            sourceBuilder.WriteLine($"namespace {compilation.Assembly.Name};");
            sourceBuilder.WriteLine();
        }
        
        sourceBuilder.WriteLine("public static class AssemblyLoader");
        sourceBuilder.WriteLine("{");
        sourceBuilder.WriteLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        sourceBuilder.WriteLine("public static void Initialize()");
        sourceBuilder.WriteLine("{");
        
        foreach (var type in types)
        {
            var typeName = type.GloballyQualifiedNonGeneric();

            if (type.IsGenericType)
            {
                typeName += $"<{new string(',', type.TypeParameters.Length - 1)}>";
            }
            
            sourceBuilder.WriteLine("try");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine($"_ = typeof({typeName}).Assembly;");
            sourceBuilder.WriteLine("}");
            sourceBuilder.WriteLine("catch");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine("// ignored");
            sourceBuilder.WriteLine("}");
        }
        
        sourceBuilder.WriteLine("}");
        sourceBuilder.WriteLine("}");
        
        context.AddSource("AssemblyLoader.g.cs", sourceBuilder.ToString());
    }

    private static INamedTypeSymbol? GetFirstType(INamespaceSymbol @namespace)
    {
        var typeMembers = @namespace.GetTypeMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic)
            .ToImmutableArray();
        
        if (!typeMembers.IsDefaultOrEmpty)
        {
            return typeMembers[0];
        }

        var namespaceMembers = @namespace.GetNamespaceMembers().ToImmutableArray();

        if (!namespaceMembers.IsDefaultOrEmpty)
        {
            foreach (var namespaceMember in namespaceMembers)
            {
                if (GetFirstType(namespaceMember) is { } namedType)
                {
                    return namedType;
                }
            }
        }
        
        return null;
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
            || publicKeyToken.SequenceEqual(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }); // mscorlib
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