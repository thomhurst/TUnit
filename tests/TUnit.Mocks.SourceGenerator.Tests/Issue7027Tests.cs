using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/7027
// Generic interface mocks are generated once as open types and closed at runtime through
// MakeGenericType, which Native AOT cannot do for value-type arguments. Closed generic interfaces
// named at a Mock.Of call site or in [assembly: GenerateMock] now get a closed factory registered
// ahead of time, so the runtime never has to close the open generated mock itself.
public class Issue7027Tests : SnapshotTestBase
{
    private const string Contracts = """
        namespace TestNamespace
        {
            public interface IRepository<T>
            {
                T? Find(int id);
            }

            public interface IPair<TKey, TValue>
            {
                TValue Get(TKey key);
            }

            public interface IPlain
            {
                int Value();
            }

            public struct Point
            {
                public int X;
            }
        }
        """;

    private const string RegistrationClassName = "ClosedGenericMockFactories";

    [Test]
    public Task Closed_Generic_Usages_Snapshot()
        => VerifyGeneratorOutput(Contracts + """

            namespace TestNamespace
            {
                public class Usage
                {
                    void M()
                    {
                        _ = TUnit.Mocks.Mock.Of<IRepository<int>>();
                        _ = TUnit.Mocks.Mock.Of<IRepository<string>>();
                        _ = TUnit.Mocks.Mock.Of<IPair<Point, System.Guid>>();
                    }
                }
            }
            """);

    [Test]
    public async Task Value_Type_Argument_Is_Registered()
    {
        var registrations = GetRegistrationSource(Usage("TUnit.Mocks.Mock.Of<IRepository<int>>()"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("global::TUnit.Mocks.MockRegistry.RegisterFactory<global::TestNamespace.IRepository<int>>(");
        await Assert.That(registrations).Contains("return global::TestNamespace.IRepository_T_MockFactory.CreateAutoMock<int>(behavior);");
    }

    [Test]
    public async Task Reference_Type_Argument_Is_Registered()
    {
        var registrations = GetRegistrationSource(Usage("TUnit.Mocks.Mock.Of<IRepository<string>>()"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::TestNamespace.IRepository<string>>(");
        await Assert.That(registrations).Contains("CreateAutoMock<string>(behavior)");
    }

    [Test]
    public async Task User_Struct_And_Multiple_Type_Arguments_Are_Registered_In_Order()
    {
        var registrations = GetRegistrationSource(Usage("TUnit.Mocks.Mock.Of<IPair<Point, System.Guid>>()"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::TestNamespace.IPair<global::TestNamespace.Point, global::System.Guid>>(");
        await Assert.That(registrations).Contains("CreateAutoMock<global::TestNamespace.Point, global::System.Guid>(behavior)");
    }

    [Test]
    public async Task Nested_Generic_Type_Arguments_Are_Registered()
    {
        var registrations = GetRegistrationSource(Usage(
            "TUnit.Mocks.Mock.Of<IRepository<System.Collections.Generic.List<int?>>>()"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains(
            "RegisterFactory<global::TestNamespace.IRepository<global::System.Collections.Generic.List<int?>>>(");
    }

    [Test]
    public async Task Repeated_And_Nullable_Annotated_Usages_Register_Once()
    {
        var registrations = GetRegistrationSource(Usage(
            "TUnit.Mocks.Mock.Of<IRepository<string>>()",
            "TUnit.Mocks.Mock.Of<IRepository<string?>>()",
            "TUnit.Mocks.Mock.Of<IRepository<string>>(TUnit.Mocks.MockBehavior.Strict)"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(CountOccurrences(registrations!, "RegisterFactory<global::TestNamespace.IRepository<string>>(")).IsEqualTo(1);
    }

    [Test]
    public async Task Registrations_Are_Emitted_In_Stable_Order()
    {
        var forward = GetRegistrationSource(Usage(
            "TUnit.Mocks.Mock.Of<IRepository<string>>()",
            "TUnit.Mocks.Mock.Of<IRepository<int>>()"));
        var reversed = GetRegistrationSource(Usage(
            "TUnit.Mocks.Mock.Of<IRepository<int>>()",
            "TUnit.Mocks.Mock.Of<IRepository<string>>()"));

        await Assert.That(forward).IsEqualTo(reversed);
    }

    [Test]
    public async Task MockRepository_Of_Is_Registered()
    {
        var registrations = GetRegistrationSource(Usage(
            "new TUnit.Mocks.MockRepository().Of<IRepository<long>>()"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::TestNamespace.IRepository<long>>(");
    }

    [Test]
    public async Task GenerateMock_Attribute_With_Closed_Generic_Is_Registered()
    {
        var registrations = GetRegistrationSource(
            "[assembly: TUnit.Mocks.GenerateMock(typeof(TestNamespace.IRepository<decimal>))]\n" + Contracts);

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::TestNamespace.IRepository<decimal>>(");
    }

    [Test]
    public async Task Constructor_Arguments_Are_Rejected_Like_The_Runtime_Fallback()
    {
        var registrations = GetRegistrationSource(Usage("TUnit.Mocks.Mock.Of<IRepository<int>>()"));

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains(
            "if (constructorArgs.Length > 0) throw new global::System.ArgumentException($\"Interface mock 'TestNamespace.IRepository<int>' does not support constructor arguments, but {constructorArgs.Length} were provided.\");");
    }

    [Test]
    public async Task Non_Generic_Interface_Emits_No_Registrations()
        => await Assert.That(GetRegistrationSource(Usage("TUnit.Mocks.Mock.Of<IPlain>()"))).IsNull();

    [Test]
    public async Task Static_Extension_Mock_Emits_No_Registrations()
        // IRepository<int>.Mock() calls the closed CreateAutoMock<int> directly and never reaches the registry.
        => await Assert.That(GetRegistrationSource(Usage("IRepository<int>.Mock()"))).IsNull();

    [Test]
    public async Task Open_Type_Argument_Emits_No_Registrations()
    {
        var source = Contracts + """

            namespace TestNamespace
            {
                public class Usage
                {
                    TUnit.Mocks.Mock<IRepository<T>> Create<T>() => TUnit.Mocks.Mock.Of<IRepository<T>>();
                }
            }
            """;

        await Assert.That(GetRegistrationSource(source)).IsNull();
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public async Task Private_Nested_Type_Argument_Is_Skipped_And_Still_Compiles()
    {
        var source = Contracts + """

            namespace TestNamespace
            {
                public class Usage
                {
                    private sealed class Secret { }
                    protected struct Hidden { }

                    void M()
                    {
                        _ = TUnit.Mocks.Mock.Of<IRepository<Secret>>();
                        _ = TUnit.Mocks.Mock.Of<IRepository<Hidden>>();
                        _ = TUnit.Mocks.Mock.Of<IRepository<int>>();
                    }
                }
            }
            """;

        var registrations = GetRegistrationSource(source);

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).DoesNotContain("Secret");
        await Assert.That(registrations).DoesNotContain("Hidden");
        await Assert.That(registrations).Contains("RegisterFactory<global::TestNamespace.IRepository<int>>(");
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public async Task Internal_Type_Argument_Is_Registered()
    {
        var source = Contracts + """

            namespace TestNamespace
            {
                internal sealed class InternalModel { }

                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<IRepository<InternalModel>>();
                }
            }
            """;

        var registrations = GetRegistrationSource(source);

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::TestNamespace.IRepository<global::TestNamespace.InternalModel>>(");
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public async Task File_Local_Type_Argument_Is_Skipped_And_Still_Compiles()
    {
        var source = Contracts + """

            namespace TestNamespace
            {
                file sealed class FileLocal { }

                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<IRepository<FileLocal>>();
                }
            }
            """;

        await Assert.That(GetRegistrationSource(source)).IsNull();
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public async Task Multi_Interface_Mock_Emits_No_Closed_Registration_For_The_Combination()
    {
        var registrations = GetRegistrationSource(Usage(
            "TUnit.Mocks.Mock.Of<IRepository<int>, System.IDisposable>()"));

        await Assert.That(registrations).IsNull();
    }

    [Test]
    public async Task Generic_Interface_Nested_In_Generic_Class_Passes_Every_Type_Argument()
    {
        var source = """
            namespace TestNamespace
            {
                public class Outer<TOuter>
                {
                    public interface IInner<TInner>
                    {
                        TOuter Left(TInner value);
                    }
                }

                public class Usage
                {
                    void M() => _ = TUnit.Mocks.Mock.Of<Outer<int>.IInner<string>>();
                }
            }
            """;

        var registrations = GetRegistrationSource(source);

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("CreateAutoMock<int, string>(behavior)");
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public async Task Global_Namespace_Interface_Is_Registered()
    {
        var source = """
            public interface IGlobalRepository<T>
            {
                T Load();
            }

            public class Usage
            {
                void M() => _ = TUnit.Mocks.Mock.Of<IGlobalRepository<int>>();
            }
            """;

        var registrations = GetRegistrationSource(source);

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::IGlobalRepository<int>>(");
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public async Task Constrained_Type_Parameters_Are_Registered()
    {
        var source = """
            namespace TestNamespace
            {
                public interface IUnmanagedSource<T> where T : unmanaged
                {
                    T Read();
                }

                public interface IClassSource<T> where T : class, new()
                {
                    T Create();
                }

                public sealed class Model { }

                public class Usage
                {
                    void M()
                    {
                        _ = TUnit.Mocks.Mock.Of<IUnmanagedSource<int>>();
                        _ = TUnit.Mocks.Mock.Of<IClassSource<Model>>();
                    }
                }
            }
            """;

        var registrations = GetRegistrationSource(source);

        await Assert.That(registrations).IsNotNull();
        await Assert.That(registrations!).Contains("RegisterFactory<global::TestNamespace.IUnmanagedSource<int>>(");
        await Assert.That(registrations).Contains("RegisterFactory<global::TestNamespace.IClassSource<global::TestNamespace.Model>>(");
        AssertCompilesWithoutErrors(source);
    }

    [Test]
    public void Closed_Generic_Usages_Compile()
        => AssertCompilesWithoutErrors(Usage(
            "TUnit.Mocks.Mock.Of<IRepository<int>>()",
            "TUnit.Mocks.Mock.Of<IRepository<string?>>()",
            "TUnit.Mocks.Mock.Of<IPair<Point, System.Guid>>()",
            "new TUnit.Mocks.MockRepository().Of<IRepository<long>>()"));

    [Test]
    public async Task Moving_A_Call_Site_Leaves_Registrations_Cached()
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var usage = Usage("TUnit.Mocks.Mock.Of<IRepository<int>>()");
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(usage, parseOptions)],
            GetCachedReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MockGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);

        var edited = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText(usage.Replace("public class Usage", "\n\npublic class Usage"), parseOptions));
        driver = driver.RunGenerators(edited);

        var reasons = driver.GetRunResult().Results.Single()
            .TrackedSteps[MockTrackingNames.ClosedGenericEmissions]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToList();

        await Assert.That(reasons).IsNotEmpty();
        foreach (var reason in reasons)
        {
            await Assert.That(reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged).IsTrue();
        }
    }

    // The pinned test Roslyn cannot parse the C# 14 extension blocks in *_MockStaticExtension.g.cs
    // (see SnapshotTestBase), so those files are excluded; every other file must be error-free.
    private static void AssertCompilesWithoutErrors(string source)
    {
        var errors = GetGeneratedCompilationErrors(source)
            .Where(error => error.Location.SourceTree?.FilePath.EndsWith("_MockStaticExtension.g.cs", StringComparison.Ordinal) != true)
            .ToList();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Generated compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static string Usage(params string[] expressions)
        => Contracts + $$"""

            namespace TestNamespace
            {
                public class Usage
                {
                    void M()
                    {
                        {{string.Join("\n            ", expressions.Select(e => $"_ = {e};"))}}
                    }
                }
            }
            """;

    private static string? GetRegistrationSource(string source)
        => RunGenerator(source).SingleOrDefault(file => file.Contains(RegistrationClassName, StringComparison.Ordinal));

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
