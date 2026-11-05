using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class MethodSourceConstructorTests
{
    [Test]
    [Arguments("SkipIfEmpty = true;", "", true)]
    [Arguments("this.DeferEnumeration = false;", "", true)]
    [Arguments("base.SkipIfEmpty = true;", "public new bool SkipIfEmpty { get; set; }", true)]
    [Arguments("SkipIfEmpty = true;", "public new bool SkipIfEmpty { get; set; }", false)]
    [Arguments("this.SkipIfEmpty = true;", "public new bool SkipIfEmpty { get; set; }", false)]
    [Arguments("Arguments = [];", "", false)]
    [Arguments("Factory = null;", "", false)]
    [Arguments("Configure();", "private void Configure() { }", false)]
    [Arguments("SkipIfEmpty = Configure();", "private bool Configure() => true;", false)]
    public async Task Only_independent_configuration_preserves_argument_mapping(string body, string members, bool expected)
    {
        var source = $$"""
            using TUnit.Core;
            public sealed class SourceAttribute : MethodDataSourceAttribute
            {
                public SourceAttribute(string memberName) : base(memberName) { {{body}} }
                {{members}}
            }
            [Source("Rows")] public class Subject { }
            """;
        var compilation = CSharpCompilation.Create("ConstructorConfiguration",
            [CSharpSyntaxTree.ParseText(source)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        await Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        var attribute = compilation.GetTypeByMetadataName("Subject")!.GetAttributes().Single();
        var helper = typeof(TestMetadataGenerator).Assembly
            .GetType("TUnit.Core.SourceGenerator.CodeGenerators.Helpers.DataSourceAttributeHelper", throwOnError: true)!
            .GetMethod("CanReadMethodSourceArguments", BindingFlags.Public | BindingFlags.Static)!;
        await Assert.That((bool)helper.Invoke(null, [attribute])!).IsEqualTo(expected);
    }
}
