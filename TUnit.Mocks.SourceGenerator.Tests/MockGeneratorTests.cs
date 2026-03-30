namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Snapshot tests for the MockGenerator source generator.
/// Each test defines input source, runs the generator, and verifies the generated output
/// against a .verified.txt snapshot file.
/// </summary>
public class MockGeneratorTests : SnapshotTestBase
{
    [Test]
    public Task Simple_Interface_With_One_Method()
    {
        var source = """
            using TUnit.Mocks;

            public interface IGreeter
            {
                string Greet(string name);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IGreeter>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Multi_Method_Interface()
    {
        var source = """
            using TUnit.Mocks;

            public interface ICalculator
            {
                int Add(int a, int b);
                int Subtract(int a, int b);
                void Reset();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ICalculator>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Properties()
    {
        var source = """
            using TUnit.Mocks;

            public interface IRepository
            {
                string Name { get; set; }
                int Count { get; }
                bool IsOpen { set; }
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IRepository>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Events()
    {
        var source = """
            using System;
            using TUnit.Mocks;

            public interface INotifier
            {
                event EventHandler<string> ItemAdded;
                void Notify(string message);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<INotifier>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_Inheriting_Multiple_Interfaces()
    {
        var source = """
            using TUnit.Mocks;

            public interface IReader
            {
                string Read();
            }

            public interface IWriter
            {
                void Write(string data);
            }

            public interface IReadWriter : IReader, IWriter
            {
                void Flush();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IReadWriter>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Async_Methods()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public interface IAsyncService
            {
                Task<string> GetValueAsync(string key);
                Task DoWorkAsync();
                ValueTask<int> ComputeAsync(int input);
                ValueTask InitializeAsync(CancellationToken ct);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IAsyncService>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Generic_Methods()
    {
        var source = """
            using System;
            using TUnit.Mocks;

            public interface IRepository
            {
                T GetById<T>(int id) where T : class;
                void Save<T>(T entity) where T : class, new();
                TResult Transform<TInput, TResult>(TInput input) where TInput : notnull where TResult : struct;
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IRepository>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Overloaded_Methods()
    {
        var source = """
            using TUnit.Mocks;

            public interface IFormatter
            {
                string Format(string value);
                string Format(int value);
                string Format(string template, string arg1);
                string Format(string template, string arg1, string arg2);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IFormatter>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Out_Ref_Parameters()
    {
        var source = """
            using TUnit.Mocks;

            public interface IDictionary
            {
                bool TryGetValue(string key, out string value);
                void Swap(ref int a, ref int b);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IDictionary>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_RefStruct_Parameters()
    {
        var source = """
            using System;
            using TUnit.Mocks;

            public interface IBufferProcessor
            {
                void Process(ReadOnlySpan<byte> data);
                int Parse(ReadOnlySpan<char> text);
                string GetName();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IBufferProcessor>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Mixed_Members()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public interface IService
            {
                string Name { get; set; }
                int Count { get; }
                event EventHandler<string> StatusChanged;
                Task<string> GetAsync(int id);
                void Process(string data);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IService>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Keyword_Parameter_Names()
    {
        var source = """
            using TUnit.Mocks;

            public interface ITest
            {
                void Test(string @event);
                string Get(int @class, string @return);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ITest>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Static_Abstract_Members()
    {
        var source = """
            using TUnit.Mocks;

            public class ClientConfig { }

            public interface IServiceFactory
            {
                string GetName();
                static abstract ClientConfig CreateDefaultConfig();
                static abstract string ServiceId { get; set; }
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IServiceFactory>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Inherited_Static_Abstract_Members()
    {
        // Tests the case where an interface inherits static abstract members from a base interface.
        // The generator resolves the call via CandidateSymbols (CS8920 workaround)
        // and generates engine-dispatching implementations in the mock impl class.
        var source = """
            using TUnit.Mocks;

            public class ClientConfig { }

            public interface IServiceBase
            {
                static abstract ClientConfig CreateDefaultConfig();
            }

            public interface IMyService : IServiceBase
            {
                string GetName();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IMyService>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Static_Abstract_Transitive_Return_Type()
    {
        // Simulates the AWS SDK scenario: a main interface returns a base interface
        // that has static abstract members. The source generator should NOT generate a
        // transitive mock for the base interface (which would trigger CS8920).
        var source = """
            using TUnit.Mocks;

            public class ClientConfig { }

            public interface IConfigProvider
            {
                ClientConfig Config { get; }
                static abstract ClientConfig CreateDefault();
            }

            public interface IMyService
            {
                string GetValue(string key);
                IConfigProvider GetConfigProvider();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IMyService>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Nullable_Reference_Type_Parameters()
    {
        var source = """
            using TUnit.Mocks;
            using System.Threading.Tasks;

            public interface IFoo
            {
                void Bar(object? baz);
                string? GetValue(string? key, int count);
                void Process(string nonNull, string? nullable, object? obj);
                Task<string?> GetAsync(string? key);
                string? NullableProp { get; set; }
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IFoo>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Partial_Mock_With_Generic_Constrained_Virtual_Methods()
    {
        var source = """
            using TUnit.Mocks;
            using System.Collections.Generic;

            public abstract class BaseService
            {
                public virtual T GetById<T>(int id) where T : class => default!;
                public virtual void Save<T>(T entity) where T : class, new() { }
                public virtual TResult Transform<TInput, TResult>(TInput input)
                    where TInput : notnull where TResult : struct
                    => default;
                public abstract IEnumerable<T> GetAll<T>() where T : class;
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<BaseService>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Partial_Mock_Filters_Internal_Virtual_Members_From_External_Assembly()
    {
        // Simulate an external assembly with internal, private protected, and public virtual members
        var externalSource = """
            namespace ExternalLib
            {
                public class ExternalClient
                {
                    public virtual string PublicMethod(string input) => input;
                    internal virtual string InternalMethod() => "internal";
                    protected internal virtual string ProtectedInternalMethod() => "protected internal";
                    private protected virtual string PrivateProtectedMethod() => "private protected";
                    public virtual string PublicProperty { get; set; } = "";
                    internal virtual string InternalProperty { get; set; } = "";

                    public ExternalClient() { }
                    internal ExternalClient(int secret) { }
                }
            }
            """;

        var externalRef = CreateExternalAssemblyReference(externalSource);

        var source = """
            using TUnit.Mocks;
            using ExternalLib;

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ExternalClient>();
                }
            }
            """;

        return VerifyGeneratorOutput(source, [externalRef]);
    }

    [Test]
    public Task Partial_Mock_Filters_Members_With_Internal_Signature_Types()
    {
        // External assembly has internal virtual methods whose signatures use internal types
        var externalSource = """
            namespace ExternalLib
            {
                internal class InternalConfig { }

                public class ServiceClient
                {
                    public virtual string GetValue(string key) => key;
                    internal virtual void Configure(InternalConfig config) { }
                    internal virtual InternalConfig GetConfig() => new InternalConfig();

                    public ServiceClient() { }
                }
            }
            """;

        var externalRef = CreateExternalAssemblyReference(externalSource);

        var source = """
            using TUnit.Mocks;
            using ExternalLib;

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ServiceClient>();
                }
            }
            """;

        return VerifyGeneratorOutput(source, [externalRef]);
    }

    [Test]
    public Task Wrap_Mock_Filters_Internal_Virtual_Members_From_External_Assembly()
    {
        var externalSource = """
            namespace ExternalLib
            {
                public class ExternalService
                {
                    public virtual string PublicMethod() => "public";
                    internal virtual string InternalMethod() => "internal";
                    public virtual string PublicProperty { get; set; } = "";
                    internal virtual string InternalProperty { get; set; } = "";

                    public ExternalService() { }
                }
            }
            """;

        var externalRef = CreateExternalAssemblyReference(externalSource);

        var source = """
            using TUnit.Mocks;
            using ExternalLib;

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Wrap(new ExternalService());
                }
            }
            """;

        return VerifyGeneratorOutput(source, [externalRef]);
    }

    [Test]
    public Task Wrap_Mock_With_Generic_Constrained_Virtual_Methods()
    {
        var source = """
            using TUnit.Mocks;

            public class Repository
            {
                public virtual T Get<T>(int id) where T : class => default!;
                public virtual void Store<T>(T item) where T : class, new() { }
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Wrap(new Repository());
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }
}
