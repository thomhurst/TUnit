using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class PolyfillGenerator : IIncrementalGenerator
{
    private class PreventCompilationTriggerOnEveryKeystrokeComparer : IEqualityComparer<Compilation>
    {
        public bool Equals(Compilation? x, Compilation? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Language == y.Language && x.AssemblyName == y.AssemblyName;
        }

        public int GetHashCode(Compilation obj)
        {
            unchecked
            {
                return (obj.Language.GetHashCode() * 397) ^ (obj.AssemblyName != null ? obj.AssemblyName.GetHashCode() : 0);
            }
        }
    }
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider
            .WithComparer(new PreventCompilationTriggerOnEveryKeystrokeComparer())
            .Combine(context.AnalyzerConfigOptionsProvider.Select((x, _) => x.GlobalOptions)), (productionContext, tuple) =>
        {
            var options = tuple.Right;

            if (options.TryGetValue("build_property.EnableTUnitPolyfills",
                    out var enableTUnitPolyfillsString)
                && bool.TryParse(enableTUnitPolyfillsString, out var enableTUnitPolyfills)
                && !enableTUnitPolyfills)
            {
                return;
            }
            
            var compilation = tuple.Left;
            
            if (compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") == null)
            {
                productionContext.AddSource("ModuleInitializerAttribute.g.cs",
                    """
                    namespace System.Runtime.CompilerServices;

                    using System;
                    using System.Diagnostics;
                    using System.Diagnostics.CodeAnalysis;

                    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
                    sealed class ModuleInitializerAttribute : Attribute;
                    """);
            }

            if (compilation.GetTypeByMetadataName("System.Diagnostics.StackTraceHiddenAttribute") == null)
            {
                productionContext.AddSource("StackTraceHiddenAttribute.g.cs",
                    """
                    namespace System.Diagnostics;

                    using System;
                    using System.Diagnostics.CodeAnalysis;

                    [AttributeUsage(
                        AttributeTargets.Class |
                        AttributeTargets.Method |
                        AttributeTargets.Constructor |
                        AttributeTargets.Struct,
                        Inherited = false)]
                    sealed class StackTraceHiddenAttribute : Attribute;
                    """);
            }
        });
    }
}