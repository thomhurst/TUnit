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
    public Task Interface_With_Nullable_Event()
    {
        // Regression test for #5424: nullable event handler types must
        // preserve nullability in generated explicit interface implementation,
        // otherwise CS8615 (nullability mismatch) is emitted.
        var source = """
            #nullable enable
            using System;
            using TUnit.Mocks;

            public interface IFoo
            {
                event EventHandler<string>? Something;
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
    public Task Interface_With_Nullable_Event_Args()
    {
        // Regression test for https://github.com/thomhurst/TUnit/issues/5425
        // Nullability of generic event handler type arguments must be preserved
        // in the generated mock to avoid CS8604.
        var source = """
            #nullable enable
            using System;
            using TUnit.Mocks;

            public interface IFoo
            {
                event EventHandler<string?> Something;
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
    public Task Interface_With_Nullable_Event_And_Nullable_Args()
    {
        // Combined regression for #5424 + #5425: both the delegate itself and
        // its generic type argument are nullable (`EventHandler<string?>?`).
        var source = """
            #nullable enable
            using System;
            using TUnit.Mocks;

            public interface IFoo
            {
                event EventHandler<string?>? Something;
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
    public Task Interface_With_Multiple_Multi_Parameter_Events()
    {
        // Regression test for #5423: RaiseEvent dispatch generated duplicate
        // `__argArray` locals in the same switch when more than one event used
        // a multi-parameter delegate (CS0128 / CS0165).
        var source = """
            using System;
            using TUnit.Mocks;

            public delegate void FirstHandler(object sender, string value);
            public delegate void SecondHandler(object sender, int value);

            public interface IDualEvents
            {
                event FirstHandler First;
                event SecondHandler Second;
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IDualEvents>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public void Generic_Interface_Inheriting_IEnumerable_In_Transitive_AutoMock_Generates_Open_Generic_Mock()
    {
        // Regression for #5567: transitive auto-mock generation for a generic
        // interface should emit a reusable open-generic mock shape, not duplicate
        // closed/open artifacts that break the user's build.
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using TUnit.Mocks;

            public interface ITest
            {
                ITestEnum<string> TestEnum { get; }
                ITestEnum<T> Create<T>();
            }

            public interface ITestEnum<T> : IEnumerable<T>
            {
                T? GetTest();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ITest>();
                }
            }
            """;

        var generated = RunGenerator(source);
        var combined = string.Join(Environment.NewLine, generated);

        if (!combined.Contains("class ITestEnum_T_MockImpl<T>", StringComparison.Ordinal)
            || !combined.Contains("class ITestEnum_T_Mock<T>", StringComparison.Ordinal)
            || !combined.Contains("RegisterOpenGenericFactory(", StringComparison.Ordinal)
            || !combined.Contains("typeof(global::ITestEnum<>)", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Expected open-generic transitive mock generation artifacts were not produced.");
        }
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
    public Task Interface_With_Generic_Method_Constraints_On_Explicit_Impl()
    {
        var source = """
            using System;
            using TUnit.Mocks;

            public interface IConstrained
            {
                T GetNotnull<T>(string key) where T : notnull;
                T GetNew<T>() where T : new();
                T GetUnmanaged<T>() where T : unmanaged;
                T GetDisposable<T>() where T : IDisposable;
                T GetClassNew<T>() where T : class, IDisposable, new();
                T GetStructDisposable<T>() where T : struct, IDisposable;
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IConstrained>();
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
    public Task Class_Implementing_Static_Abstract_Interface()
    {
        // Mirrors the T15 KitchenSink shape: a class implementing an interface that has a
        // static-abstract member plus an instance virtual member. The generator must NOT
        // emit a MockBridge interface for class targets (CS0527 / CS0540); the class
        // already provides the concrete static impl, the mock only overrides the
        // instance-virtual surface.
        //
        // The verified snapshot for this test intentionally OMITS a `_MockBridge.g.cs`
        // file section — that absence is the assertion. Class targets must not get
        // bridge generation, unlike the interface-target variants in this file which
        // do produce a bridge.
        var source = """
            using TUnit.Mocks;

            public interface IStaticAbstractFactory
            {
                static abstract IStaticAbstractFactory Create();
                int InstanceValue { get; }
            }

            public class StaticAbstractImpl : IStaticAbstractFactory
            {
                public static IStaticAbstractFactory Create() => new StaticAbstractImpl();
                public virtual int InstanceValue => 99;
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = StaticAbstractImpl.Mock();
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

    // Regression for https://github.com/thomhurst/TUnit/issues/5455 — public virtual properties
    // whose setters are individually inaccessible (internal/private) must emit getter-only overrides.
    // `Reason` is the control: `protected internal set` is reachable via the mock's inheritance, so
    // its setter must still be emitted.
    [Test]
    public Task Partial_Mock_Omits_Inaccessible_Property_Setters()
    {
        var externalSource = """
            namespace ExternalLib
            {
                public class ExternalResponse
                {
                    public virtual int StatusCode { get; internal set; }
                    public virtual bool IsSuccess { get; private set; }
                    public virtual string Reason { get; protected internal set; } = "";
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
                    var mock = Mock.Of<ExternalResponse>();
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

    [Test]
    public Task GenerateMock_Attribute_With_Concrete_Class()
    {
        var source = """
            using TUnit.Mocks;

            [assembly: GenerateMock(typeof(MyService))]

            public class MyService
            {
                public virtual string GetValue() => "real";
                public virtual void DoWork() { }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Class_With_Constructor_Parameters_Extension_Discovery()
    {
        var source = """
            using TUnit.Mocks;

            public class MyService
            {
                public MyService() { }
                public MyService(string connectionString, int timeout) { }
                public MyService(string connectionString, int timeout, bool verbose) { }
                public virtual string GetValue() => "real";
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = MyService.Mock();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Class_With_Same_Arity_Constructor_Overloads()
    {
        var source = """
            using TUnit.Mocks;

            public class MyService
            {
                public MyService(string name) { }
                public MyService(int id) { }
                public MyService(string host, int port) { }
                public MyService(int timeout, bool verbose) { }
                public virtual string GetValue() => "real";
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = MyService.Mock("test");
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Static_Extension_Discovery_Without_Mock_Of()
    {
        // IFoo.Mock() should trigger generation even without Mock.Of<IFoo>()
        var source = """
            using TUnit.Mocks;

            public interface INotifier
            {
                void Notify(string message);
                string GetStatus();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = INotifier.Mock();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Generic_Interface_Extension_Discovery()
    {
        var source = """
            using TUnit.Mocks;

            public interface IRepository<T>
            {
                T GetById(int id);
                void Save(T entity);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = IRepository<string>.Mock();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Generic_Interface_With_Enum_Type_Argument()
    {
        var source = """
            using TUnit.Mocks;

            namespace Sandbox
            {
                public enum SomeEnum
                {
                    Value1,
                    Value2
                }

                public interface IFoo<T>
                {
                    T Value { get; }
                }

                public class TestUsage
                {
                    void M()
                    {
                        var mock = IFoo<SomeEnum>.Mock();
                    }
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Generic_Interface_With_Class_Type_Argument()
    {
        var source = """
            using TUnit.Mocks;

            namespace Sandbox
            {
                public class Bar
                {
                    public string Name { get; set; } = "";
                }

                public interface IFoo<T>
                {
                    T Value { get; }
                    void Process(T item);
                }

                public class TestUsage
                {
                    void M()
                    {
                        var mock = IFoo<Bar>.Mock();
                    }
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Generic_Interface_With_Nested_Namespace_Type_Argument()
    {
        var source = """
            using TUnit.Mocks;

            namespace Outer.Inner
            {
                public class Config
                {
                    public int Timeout { get; set; }
                }
            }

            namespace Sandbox
            {
                public interface IService<T>
                {
                    T GetConfig();
                    void Apply(T config);
                }

                public class TestUsage
                {
                    void M()
                    {
                        var mock = IService<Outer.Inner.Config>.Mock();
                    }
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Generic_Interface_With_Multiple_Non_Builtin_Type_Arguments()
    {
        var source = """
            using TUnit.Mocks;

            namespace Sandbox
            {
                public class Entity
                {
                    public int Id { get; set; }
                }

                public enum Status
                {
                    Active,
                    Inactive
                }

                public interface IMapper<TIn, TOut>
                {
                    TOut Map(TIn input);
                }

                public class TestUsage
                {
                    void M()
                    {
                        var mock = IMapper<Entity, Status>.Mock();
                    }
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Generic_Interface_With_Nested_Generic_Type_Argument()
    {
        var source = """
            using System.Collections.Generic;
            using TUnit.Mocks;

            namespace Sandbox
            {
                public class Item
                {
                    public string Name { get; set; } = "";
                }

                public interface IProvider<T>
                {
                    T Get();
                }

                public class TestUsage
                {
                    void M()
                    {
                        var mock = IProvider<List<Item>>.Mock();
                    }
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Unconstrained_Nullable_Generic()
    {
        var source = """
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public interface IFoo
            {
                Task<T?> DoSomethingAsync<T>();
                T? GetValue<T>();
                (T?, string) GetPair<T>();
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
    public Task Interface_Inheriting_IEnumerable_Generic()
    {
        var source = """
            using System.Collections.Generic;
            using TUnit.Mocks;

            public interface ITestEnum<T> : IEnumerable<T>
            {
            }

            public interface ITest
            {
                ITestEnum<string> TestEnum { get; }
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ITestEnum<string>>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_Directly_Inheriting_IEnumerable()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;
            using TUnit.Mocks;

            public interface ICustomCollection : IEnumerable<int>
            {
                int Count { get; }
                void Add(int item);
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<ICustomCollection>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_With_Obsolete_Members()
    {
        // Regression for #5626: members marked [Obsolete] on the source interface or
        // base class previously caused CS0612/CS0618/CS0672 warnings to leak from the
        // generated mock into consumer builds (a blocker for TreatWarningsAsErrors).
        // The fix copies the [Obsolete] attribute onto every generated forward and
        // override, since a member marked [Obsolete] may freely call other obsolete
        // members without warning.
        var source = """
            using System;
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public interface IDialogService
            {
                [Obsolete("Use ShowAsync(options) instead")]
                string? Show(string? title, string? message);

                [Obsolete]
                Task<string?> ShowPanel<TData>(TData? data) where TData : class;

                [Obsolete("Removed", true)]
                event EventHandler<string?>? Opened;

                string? Greeting { [Obsolete] get; [Obsolete] set; }

                // Asymmetric accessor cases: only the marked accessor should carry [Obsolete]
                // on the generated forward, otherwise the unmarked accessor would gain a
                // spurious CS0618 warning at the consumer call site.
                string? Headline { [Obsolete] get; set; }
                string? Subtitle { get; [Obsolete] set; }

                // Exercises the message-escape path: embedded quotes and backslashes
                // must round-trip through the generated attribute literal verbatim.
                [Obsolete("Use \"NewMethod\" in C:\\New\\Path")]
                string? WithTrickyChars();
            }

            public abstract class BaseDialog
            {
                [Obsolete]
                public virtual string? Compute(string? input) => input;
            }

            public class TestUsage
            {
                void M()
                {
                    var dialog = Mock.Of<IDialogService>();
                    var partial = Mock.Of<BaseDialog>();
                }
            }
            """;

        AssertGeneratedCodeHasNoObsoleteWarnings(source);
        AssertGeneratedCodeHasNoNullableWarnings(source);
        return VerifyGeneratorOutput(source);
    }

#if NET6_0_OR_GREATER
    [Test]
    public Task Interface_With_Obsolete_DiagnosticId_NamedArgs()
    {
        // C# 10+ DiagnosticId and UrlFormat named arguments on [Obsolete] were added in
        // .NET 5; they don't exist on .NET Framework 4.7.2's mscorlib. This test is gated
        // on net6+ so the test compilation can resolve the named arguments against a
        // mscorlib that has them. The generator's named-arg preservation path is exercised
        // here to keep the regression guard for the bot review #2 fix on supported TFMs.
        var source = """
            using System;
            using TUnit.Mocks;

            public interface IDeprecatedApi
            {
                [Obsolete("Replaced", DiagnosticId = "CUSTOM001", UrlFormat = "https://example.test/{0}")]
                string? WithDiagnosticId();
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IDeprecatedApi>();
                }
            }
            """;

        AssertGeneratedCodeHasNoObsoleteWarnings(source);
        AssertGeneratedCodeHasNoNullableWarnings(source);
        return VerifyGeneratorOutput(source);
    }
#endif

    [Test]
    public Task Interface_FluentUI_Shape_Nullable_Warnings()
    {
        // Investigation for the CS8600/CS8604 portion of #5626. The reporter claimed
        // these warnings fire against Microsoft.FluentUI.AspNetCore.Components.IDialogService.
        // These are the exact patterns from FluentUI that earlier synthetic repros missed:
        // unconstrained Task<T?> returns, class-constrained generics with nullable returns,
        // and events with mixed-nullability delegate type arguments.
        var source = """
            using System;
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public interface IDialogReference
            {
                // Unconstrained generic + Task<T?> return: classic CS8600 trigger.
                Task<T?> GetReturnValueAsync<T>();
            }

            public interface IDialogContentComponent { }
            public interface IDialogContentComponent<TContent> : IDialogContentComponent { TContent Content { get; set; } }
            public class DialogParameters { }
            public class DialogParameters<TContent> : DialogParameters where TContent : class { public TContent Content { get; set; } = default!; }

            public partial interface IDialogService
            {
                // Nullable return + class-constrained generic
                Task<IDialogReference?> UpdateDialogAsync<TData>(string id, DialogParameters<TData> parameters) where TData : class;

                // Non-nullable return + different generic-constraint shape
                Task<IDialogReference> ShowDialogAsync<TDialog>(object data, DialogParameters parameters) where TDialog : IDialogContentComponent;

                // Event with mixed nullable/non-nullable delegate type arguments
                event Action<IDialogReference, Type?, DialogParameters, object>? OnShow;
            }

            public class TestUsage
            {
                void M()
                {
                    var dialog = Mock.Of<IDialogService>();
                    var refs = Mock.Of<IDialogReference>();
                }
            }
            """;

        AssertGeneratedCodeHasNoNullableWarnings(source);
        return VerifyGeneratorOutput(source);
    }

    [Test]
    public Task Interface_Inheriting_Nested_Generic_IEnumerable()
    {
        var source = """
            using System.Collections.Generic;
            using TUnit.Mocks;

            public interface IPagedResult<T> : IEnumerable<T>
            {
                int TotalCount { get; }
                int PageSize { get; }
            }

            public class TestUsage
            {
                void M()
                {
                    var mock = Mock.Of<IPagedResult<string>>();
                }
            }
            """;

        return VerifyGeneratorOutput(source);
    }
}
