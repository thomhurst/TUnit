using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Tests;

public class DataSourceAttributeDetectionTests
{
    [Test]
    [Arguments("DirectSource", true)]
    [Arguments("InheritedSource", true)]
    [Arguments("IndirectSource", true)]
    [Arguments("GenericSource<int>", true)]
    [Arguments("Unrelated", false)]
    [Arguments("OtherNamespace", false)]
    [Arguments("PrefixedNamespace", false)]
    [Arguments("GenericInterface", false)]
    [Arguments("NestedInterface", false)]
    public async Task RecognizesDataSourceInterfaces(string attributeName, bool expected)
    {
        var source = """
            namespace TUnit.Core
            {
                public interface IDataSourceAttribute { }
                public interface IDataSourceAttribute<T> { }
                public interface IIndirect : IDataSourceAttribute { }
            }
            namespace Other { public interface IDataSourceAttribute { } }
            namespace Other.TUnit.Core { public interface IDataSourceAttribute { } }
            public class Container { public interface IDataSourceAttribute { } }
            public class DirectSource : System.Attribute, TUnit.Core.IDataSourceAttribute { }
            public class InheritedSource : DirectSource { }
            public class IndirectSource : System.Attribute, TUnit.Core.IIndirect { }
            public class GenericSource<T> : System.Attribute, TUnit.Core.IDataSourceAttribute { }
            public class Unrelated : System.Attribute, System.ICloneable
            {
                public object Clone() => this;
            }
            public class OtherNamespace : System.Attribute, Other.IDataSourceAttribute { }
            public class PrefixedNamespace : System.Attribute, Other.TUnit.Core.IDataSourceAttribute { }
            public class GenericInterface : System.Attribute, TUnit.Core.IDataSourceAttribute<int> { }
            public class NestedInterface : System.Attribute, Container.IDataSourceAttribute { }
            """ + $"\n[{attributeName}] public class Subject {{ }}";

        var attribute = await GetSubjectAttribute(source);
        await Assert.That(attribute.IsDataSourceAttribute()).IsEqualTo(expected);
    }

    [Test]
    public async Task NestedInterfaceWithMatchingDisplayName_PreservesExistingBehavior()
    {
        // Display-name matching historically accepts this spelling even though Core is a type.
        const string source = """
            namespace TUnit
            {
                public class Core { public interface IDataSourceAttribute { } }
            }
            public class Source : System.Attribute, TUnit.Core.IDataSourceAttribute { }
            [Source] public class Subject { }
            """;

        var attribute = await GetSubjectAttribute(source);
        await Assert.That(attribute.IsDataSourceAttribute()).IsTrue();
    }

    [Test]
    public async Task NullAttribute_IsNotDataSource()
    {
        await Assert.That(AttributeDataExtensions.IsDataSourceAttribute(null)).IsFalse();
    }

    private static async Task<AttributeData> GetSubjectAttribute(string source)
    {
        var compilation = CSharpCompilation.Create("AttributeDetection",
            [CSharpSyntaxTree.ParseText(source)], ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        await Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)).IsEmpty();
        return compilation.GetTypeByMetadataName("Subject")!.GetAttributes().Single();
    }
}
