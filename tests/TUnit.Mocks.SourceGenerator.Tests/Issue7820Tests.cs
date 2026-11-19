using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class Issue7820Tests : SnapshotTestBase
{
    private const string Contracts = """
        #nullable enable
        using System;
        using System.Threading.Tasks;

        public class BaseAct { }
        public class Primary { }

        public interface IBaseFin
        {
            Task<(T? act, string error)> ActFromInvoice<T>() where T : BaseAct, new();
        }

        public interface IFin : IBaseFin
        {
            new Task<(T? act, string error)> ActFromInvoice<T>() where T : BaseAct, new();
            T? Find<T>() where T : BaseAct?;
            void Accept<T>(T? value) where T : Delegate;
        }
        """;

    [Test]
    [Arguments("BaseAct")]
    [Arguments("BaseAct?")]
    [Arguments("BaseAct, new()")]
    [Arguments("BaseAct, IDisposable, new()")]
    [Arguments("Delegate")]
    [Arguments("class")]
    [Arguments("class?")]
    [Arguments("IDisposable")]
    [Arguments("notnull")]
    [Arguments("struct")]
    [Arguments("unmanaged")]
    [Arguments("new()")]
    [Arguments("")]
    public void Nullable_Generic_Method_Signatures_Compile(string constraints)
    {
        var clause = constraints.Length == 0 ? "" : $"where T : {constraints}";
        var source = $$"""
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public class BaseAct : IDisposable { public void Dispose() { } }

            public interface IFin
            {
                Task<(T? act, string error)> ActFromInvoice<T>() {{clause}};
                T? Find<T>() {{clause}};
                T?[] FindAll<T>() {{clause}};
                void Accept<T>(T? value) {{clause}};
                T? Echo<T>(T? value) {{clause}};
                void Exchange<T>(ref T? value, out T? previous) {{clause}};
            }

            public class Usage
            {
                public object Create() => Mock.Of<IFin>();
            }
            """;

        AssertMockImplementationsCompile(source);
        AssertGeneratedCodeHasNoNullableWarnings(source);
    }

    [Test]
    public void Multiple_Type_Parameters_Preserve_Their_Own_Constraint_Kind()
    {
        const string source = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public class BaseAct { }
            public interface IFin
            {
                Task<(T? act, U? data)> Read<T, U>() where T : BaseAct, new();
                T? Echo<T, U>(U? value) where T : BaseAct where U : struct;
                T? Find<T, U>() where T : U where U : BaseAct;
            }

            public class Usage
            {
                public object Create() => Mock.Of<IFin>();
            }
            """;

        AssertMockImplementationsCompile(source);
        AssertGeneratedCodeHasNoNullableWarnings(source);
    }

    [Test]
    [Arguments(false, "Mock.Of<IFin>()")]
    [Arguments(true, "Mock.Of<IFin>()")]
    [Arguments(false, "Mock.Of<Primary, IFin>()")]
    [Arguments(true, "Mock.Of<Primary, IFin>()")]
    public void Inherited_Nullable_Generic_Methods_Compile(bool fromMetadata, string expression)
    {
        var usage = $$"""
            public class Usage
            {
                public object Create() => TUnit.Mocks.{{expression}};
            }
            """;

        if (fromMetadata)
        {
            var reference = CreateExternalAssemblyReference(Contracts);
            AssertMockImplementationsCompile(usage, [reference]);
            AssertGeneratedCodeHasNoNullableWarnings(usage, [reference]);
        }
        else
        {
            AssertMockImplementationsCompile(Contracts + usage);
            AssertGeneratedCodeHasNoNullableWarnings(Contracts + usage);
        }
    }

    [Test]
    public Task Base_Class_Constrained_Nullable_Generic_Methods()
        => VerifyGeneratorOutput(Contracts + """

            public class Usage
            {
                public object Create() => TUnit.Mocks.Mock.Of<IFin>();
            }
            """);

    private static void AssertMockImplementationsCompile(
        string source, IEnumerable<MetadataReference>? additionalReferences = null)
    {
        // The pinned test Roslyn predates C# 14 extension blocks. Runtime regressions compile
        // the complete output with the SDK; every other generated file must compile here.
        var errors = GetGeneratedCompilationErrors(source, additionalReferences)
            .Where(error => error.Location.SourceTree?.FilePath.EndsWith("_MockStaticExtension.g.cs", StringComparison.Ordinal) != true)
            .ToList();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Generated compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}
