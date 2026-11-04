using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Analyzers.Extensions;
using Assert = TUnit.Assertions.Assert;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GenericTupleArrayMethodDataSourceTests : BaseAnalyzerTests
{
    [Test]
    public async Task RankOneNonSzArrayDoesNotBindToOpenVector()
    {
        // C# cannot spell T[*]. Import a field with that signature from metadata.
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("ArrayShapes"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("ArrayShapes"), new Version(1, 0), default, default, default, default);
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
            default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        var type = metadata.AddTypeDefinition(TypeAttributes.Public, default, metadata.GetOrAddString("Shapes`1"),
            default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        metadata.AddGenericParameter(type, GenericParameterAttributes.None, metadata.GetOrAddString("T"), 0);
        // FIELD, ARRAY, VAR 0, rank 1, no sizes, no lower bounds.
        metadata.AddFieldDefinition(FieldAttributes.Public, metadata.GetOrAddString("NonVector"),
            metadata.GetOrAddBlob(new byte[] { 0x06, 0x14, 0x13, 0x00, 0x01, 0x00, 0x00 }));
        var pe = new ManagedPEBuilder(new PEHeaderBuilder(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var image = new BlobBuilder();
        pe.Serialize(image);
        var compilation = CSharpCompilation.Create("Consumer",
            references: [MetadataReference.CreateFromImage(image.ToImmutableArray())]);
        var shapes = compilation.GetTypeByMetadataName("Shapes`1")!;
        var nonVector = (IArrayTypeSymbol)((IFieldSymbol)shapes.GetMembers("NonVector").Single()).Type;
        var vector = compilation.CreateArrayTypeSymbol(shapes.TypeParameters[0]);

        await Assert.That(nonVector.Rank).IsEqualTo(1);
        await Assert.That(nonVector.IsSZArray).IsFalse();
        await Assert.That(compilation.HasImplicitConversionOrGenericParameter(nonVector, vector)).IsFalse();
        await Assert.That(compilation.HasImplicitConversionOrGenericParameter(vector, nonVector)).IsFalse();
    }

    [Test]
    [Arguments("(int, int)[]", "(T, long)[]", true)]
    [Arguments("(int, string[])[]", "(T, object[])[]", true)]
    [Arguments("(int, int)[]", "(T, int)[]", false)]
    [Arguments("int[]", "T[]", false)]
    [Arguments("int[][]", "T[][]", false)]
    [Arguments("string[][]", "T[][]", false)]
    [Arguments("int", "(T, int[])", true)]
    [Arguments("int[]", "(T, int[])", true)]
    [Arguments("int[]", "(T, int[])[]", true)]
    [Arguments("(int, int, int[])", "(T, int[])", true)]
    [Arguments("int[,]", "T[,]", false)]
    [Arguments("int[]", "T[,]", true)]
    [Arguments("int", "T[]", true)]
    [Arguments("int[]", "string[]", true)]
    [Arguments("(int, int[])", "(T, T[])", false)]
    [Arguments("(int, (int, int[][]))", "(T, (T, T[][]))", false)]
    [Arguments("(int, string[])", "(T, int[])", true)]
    [Arguments("List<int>[]", "IEnumerable<T>[]", false)]
    [Arguments("IConsumer<string, object>[]", "IConsumer<T, string>[]", false)]
    [Arguments("(int, string[])", "(T, object[])", false)]
    public async Task TupleArrayElements(string sourceElement, string targetElement, bool expectDiagnostic)
    {
        var attribute = expectDiagnostic ? "{|#0:MethodDataSource(nameof(Row))|}" : "MethodDataSource(nameof(Row))";
        var source = $$"""
            #pragma warning disable TUnit0046 // Mutable array rows are intentional in these binding tests.
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Core.Interfaces;
            public interface IConsumer<A, in B> { }
            public class C
            {
                public static (Fixture, CancellationToken, {{sourceElement}}) Row() => default;
                [Test, {{attribute}}]
                public void Test<T>((Fixture, CancellationToken, {{targetElement}}) row) { }
                public struct Fixture : IAsyncInitializer
                {
                    public Task InitializeAsync() => Task.CompletedTask;
                }
            }
            """;

        if (expectDiagnostic)
        {
            await Verifier.VerifyAnalyzerAsync(source,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
                    .WithArguments($"Fixture, CancellationToken, {sourceElement}", $"(Fixture, CancellationToken, {targetElement})"));
        }
        else
        {
            await Verifier.VerifyAnalyzerAsync(source);
        }
    }
}
