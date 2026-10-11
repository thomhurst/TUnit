using System.Runtime.CompilerServices;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7027.
/// Closed generic interfaces named at a <c>Mock.Of</c> call site are registered as closed factories
/// by the source generator, so they never go through <c>MakeGenericType</c> — which Native AOT
/// cannot do for value-type arguments. These tests also run in the Native AOT CI job.
/// </summary>
public class Issue7027Tests
{
    public interface IStore7027<T>
    {
        T? Find(int id);
        void Save(T item);
    }

    public interface ILookup7027<TKey, TValue>
    {
        TValue Get(TKey key);
    }

    public interface IFallbackOnly7027<T>
    {
        T Read();
    }

    public readonly record struct Money7027(decimal Amount, string Currency);

    public readonly record struct FallbackValue7027(int Value);

    public enum Color7027
    {
        Red,
        Green,
    }

    [Test]
    public async Task Value_Type_Argument_Mock_Returns_Configured_Value()
    {
        var mock = Mock.Of<IStore7027<int>>();
        mock.Find(1).Returns(42);

        await Assert.That(mock.Object.Find(1)).IsEqualTo(42);
        await Assert.That(mock.Object.Find(2)).IsEqualTo(0);
    }

    [Test]
    public async Task Reference_Type_Argument_Mock_Returns_Configured_Value()
    {
        var mock = Mock.Of<IStore7027<string>>();
        mock.Find(1).Returns("one");

        await Assert.That(mock.Object.Find(1)).IsEqualTo("one");
    }

    [Test]
    public async Task User_Struct_Argument_Mock_Returns_Configured_Value()
    {
        var mock = Mock.Of<IStore7027<Money7027>>();
        mock.Find(7).Returns(new Money7027(9.99m, "EUR"));

        await Assert.That(mock.Object.Find(7)).IsEqualTo(new Money7027(9.99m, "EUR"));
    }

    [Test]
    public async Task Multiple_Value_Type_Arguments_Mock_Returns_Configured_Value()
    {
        var key = Guid.NewGuid();
        var mock = Mock.Of<ILookup7027<Guid, Color7027>>();
        mock.Get(key).Returns(Color7027.Green);

        await Assert.That(mock.Object.Get(key)).IsEqualTo(Color7027.Green);
        await Assert.That(mock.Object.Get(Guid.Empty)).IsEqualTo(Color7027.Red);
    }

    [Test]
    public async Task Nested_Generic_Value_Type_Argument_Mock_Returns_Configured_Value()
    {
        var mock = Mock.Of<IStore7027<List<long?>>>();
        mock.Find(1).Returns([1L, null]);

        await Assert.That(mock.Object.Find(1)).IsEquivalentTo(new List<long?> { 1L, null });
    }

    [Test]
    public async Task Value_Type_Argument_Mock_Verifies_Calls()
    {
        var mock = Mock.Of<IStore7027<double>>();

        mock.Object.Save(1.5);
        mock.Object.Save(1.5);

        mock.Save(1.5).WasCalled(Times.Exactly(2));
        mock.Save(2.5).WasNeverCalled();
        await Assert.That(mock.Object).IsNotNull();
    }

    [Test]
    public async Task Value_Type_Argument_Mock_Honours_Strict_Behavior()
    {
        var mock = Mock.Of<IStore7027<short>>(MockBehavior.Strict);

        await Assert.That(() => mock.Object.Find(1)).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Value_Type_Argument_Mock_From_Repository_Returns_Configured_Value()
    {
        var repository = new MockRepository();
        var mock = repository.Of<IStore7027<byte>>();
        mock.Find(3).Returns((byte)3);

        await Assert.That(mock.Object.Find(3)).IsEqualTo((byte)3);
    }

    [Test]
    public async Task Closed_Registration_Rejects_Constructor_Arguments()
    {
        var exception = Assert.Throws<ArgumentException>(() => Mock.Of<IStore7027<ushort>>(MockBehavior.Loose, 1));

        await Assert.That(exception.Message).Contains("does not support constructor arguments, but 1 were provided");
    }

    [Test]
    public async Task Each_Closed_Usage_Is_Registered_By_Generated_Code()
    {
        // Factories built by the MakeGenericType fallback are closures declared in TUnit.Mocks;
        // generated closed registrations are lambdas declared in this test assembly.
        foreach (var type in new[]
                 {
                     typeof(IStore7027<int>),
                     typeof(IStore7027<string>),
                     typeof(IStore7027<Money7027>),
                     typeof(ILookup7027<Guid, Color7027>),
                     typeof(IStore7027<List<long?>>),
                     typeof(IStore7027<double>),
                     typeof(IStore7027<short>),
                     typeof(IStore7027<byte>),
                     typeof(IStore7027<ushort>),
                 })
        {
            await Assert.That(MockRegistry.TryGetFactory(type, out var factory)).IsTrue();
            await Assert.That(factory.Target?.GetType().Assembly).IsEqualTo(typeof(Issue7027Tests).Assembly);
        }
    }

    [Test]
    public async Task Type_Argument_Only_Known_At_Runtime_Uses_Fallback()
    {
        // The generator only sees IFallbackOnly7027<T>, so this goes through MakeGenericType.
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            var mock = CreateFallbackOnly<FallbackValue7027>();
            mock.Read().Returns(new FallbackValue7027(5));

            await Assert.That(mock.Object.Read()).IsEqualTo(new FallbackValue7027(5));
            return;
        }

        // On Native AOT the closed generated types may not exist; if so, the error must say how to fix it.
        try
        {
            var mock = CreateFallbackOnly<FallbackValue7027>();
            await Assert.That(mock.Object).IsNotNull();
        }
        catch (NotSupportedException exception)
        {
            await Assert.That(exception.Message).Contains("Native AOT");
            await Assert.That(exception.Message).Contains(".Mock()");
        }
    }

    public class Outer7027<TOuter>
    {
        public interface IInner<TInner>;

        public sealed class Nested;
    }

    [Test]
    [Arguments(typeof(int), "System.Int32")]
    [Arguments(typeof(IStore7027<int>), "TUnit.Mocks.Tests.Issue7027Tests.IStore7027<System.Int32>")]
    [Arguments(typeof(ILookup7027<Guid, Color7027>), "TUnit.Mocks.Tests.Issue7027Tests.ILookup7027<System.Guid, TUnit.Mocks.Tests.Issue7027Tests.Color7027>")]
    [Arguments(typeof(Outer7027<int>.IInner<string>), "TUnit.Mocks.Tests.Issue7027Tests.Outer7027<System.Int32>.IInner<System.String>")]
    [Arguments(typeof(IStore7027<Outer7027<long>.Nested>), "TUnit.Mocks.Tests.Issue7027Tests.IStore7027<TUnit.Mocks.Tests.Issue7027Tests.Outer7027<System.Int64>.Nested>")]
    [Arguments(typeof(IStore7027<int[,]>), "TUnit.Mocks.Tests.Issue7027Tests.IStore7027<System.Int32[,]>")]
    [Arguments(typeof(IStore7027<List<int?>>), "TUnit.Mocks.Tests.Issue7027Tests.IStore7027<System.Collections.Generic.List<System.Nullable<System.Int32>>>")]
    public async Task Native_Aot_Fix_Suggestion_Uses_CSharp_Type_Syntax(Type type, string expected)
    {
        // The NotSupportedException tells users to paste this name into Mock.Of<...>() or typeof(...).
        await Assert.That(MockRegistry.FormatTypeName(type)).IsEqualTo(expected);
    }

    [Test]
    public async Task Native_Aot_Fallback_Message_Recommends_The_Static_Extension()
    {
        var message = MockRegistry.GetNativeAotFallbackMessage(typeof(IStore7027<Outer7027<int>.Nested>));

        await Assert.That(message).Contains("TUnit.Mocks.Tests.Issue7027Tests.IStore7027<TUnit.Mocks.Tests.Issue7027Tests.Outer7027<System.Int32>.Nested>.Mock()");
        await Assert.That(message).Contains("[assembly: GenerateMock(typeof(TUnit.Mocks.Tests.Issue7027Tests.IStore7027<TUnit.Mocks.Tests.Issue7027Tests.Outer7027<System.Int32>.Nested>))]");
        await Assert.That(message.IndexOf(".Mock()", StringComparison.Ordinal))
            .IsLessThan(message.IndexOf("GenerateMock", StringComparison.Ordinal));
    }

    private readonly record struct PrivateValue7027(int Value);

    [Test]
    public async Task Static_Extension_Works_With_Private_Nested_Value_Type_Argument()
    {
        // The generator cannot register IStore7027<PrivateValue7027> from a top-level class, but the
        // generic extension infers T at this call site, so no reflection or registration is needed.
        var mock = IStore7027<PrivateValue7027>.Mock();
        mock.Find(1).Returns(new PrivateValue7027(11));

        await Assert.That(mock.Object.Find(1)).IsEqualTo(new PrivateValue7027(11));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Mock<IFallbackOnly7027<T>> CreateFallbackOnly<T>() => Mock.Of<IFallbackOnly7027<T>>();
}

/// <summary>
/// The opt-in <see cref="IDefaultValueProvider"/> lookup used to be duplicated across every typed
/// <c>MockEngine</c> arity behind <c>#pragma</c> suppressions, which ILC ignores. It now lives in
/// one helper carrying <c>[UnconditionalSuppressMessage]</c>; these tests pin its behaviour on every
/// arity (typed 1–8, untyped 0 and 9+) and on both the plain and auto-mock return paths (#7027).
/// </summary>
public class Issue7027DefaultValueProviderTests
{
    public interface IChild7027
    {
        int Id();
    }

    public interface IArity7027
    {
        string S0();
        string S1(int a);
        string S2(int a, int b);
        string S3(int a, int b, int c);
        string S4(int a, int b, int c, int d);
        string S5(int a, int b, int c, int d, int e);
        string S6(int a, int b, int c, int d, int e, int f);
        string S7(int a, int b, int c, int d, int e, int f, int g);
        string S8(int a, int b, int c, int d, int e, int f, int g, int h);
        string S9(int a, int b, int c, int d, int e, int f, int g, int h, int i);

        IChild7027 C0();
        IChild7027 C1(int a);
        IChild7027 C2(int a, int b);
        IChild7027 C3(int a, int b, int c);
        IChild7027 C4(int a, int b, int c, int d);
        IChild7027 C5(int a, int b, int c, int d, int e);
        IChild7027 C6(int a, int b, int c, int d, int e, int f);
        IChild7027 C7(int a, int b, int c, int d, int e, int f, int g);
        IChild7027 C8(int a, int b, int c, int d, int e, int f, int g, int h);
        IChild7027 C9(int a, int b, int c, int d, int e, int f, int g, int h, int i);
    }

    private sealed class Child : IChild7027
    {
        public int Id() => 7027;
    }

    private sealed class Provider(Func<Type, bool> canProvide, Func<Type, object?> getDefaultValue) : IDefaultValueProvider
    {
        public bool CanProvide(Type type) => canProvide(type);

        public object? GetDefaultValue(Type type) => getDefaultValue(type);
    }

    [Test]
    public async Task Provider_Value_Is_Returned_On_Every_Arity()
    {
        var calc = CreateWith(new Provider(_ => true, type => type == typeof(string) ? "provided" : new Child()));

        foreach (var value in Strings(calc))
        {
            await Assert.That(value).IsEqualTo("provided");
        }

        foreach (var child in Children(calc))
        {
            await Assert.That(child.Id()).IsEqualTo(7027);
        }
    }

    [Test]
    public async Task Provider_Null_Returns_Default_On_Every_Arity()
    {
        var calc = CreateWith(new Provider(_ => true, _ => null));

        foreach (var value in Strings(calc))
        {
            await Assert.That(value).IsNull();
        }

        foreach (var child in Children(calc))
        {
            await Assert.That(child).IsNull();
        }
    }

    [Test]
    public async Task Provider_Value_Of_Wrong_Type_Falls_Through_On_Every_Arity()
    {
        // A mismatched value is ignored: strings get the generated smart default, interfaces get an auto-mock.
        var calc = CreateWith(new Provider(_ => true, _ => 123));

        foreach (var value in Strings(calc))
        {
            await Assert.That(value).IsEqualTo("");
        }

        foreach (var child in Children(calc))
        {
            await Assert.That(child).IsNotNull();
            await Assert.That(child.Id()).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Provider_That_Cannot_Provide_Is_Not_Asked_On_Every_Arity()
    {
        var asked = 0;
        var calc = CreateWith(new Provider(_ => false, _ =>
        {
            Interlocked.Increment(ref asked);
            return "unexpected";
        }));

        foreach (var value in Strings(calc))
        {
            await Assert.That(value).IsEqualTo("");
        }

        foreach (var child in Children(calc))
        {
            await Assert.That(child).IsNotNull();
        }

        await Assert.That(asked).IsEqualTo(0);
    }

    [Test]
    public async Task Provider_Is_Consulted_Per_Return_Type()
    {
        var calc = CreateWith(new Provider(type => type == typeof(string), _ => "only-strings"));

        await Assert.That(calc.S3(1, 2, 3)).IsEqualTo("only-strings");
        await Assert.That(calc.C3(1, 2, 3)).IsNotNull();
        await Assert.That(calc.C3(1, 2, 3)).IsNotTypeOf<Child>();
    }

    [Test]
    public async Task Setup_Wins_Over_Provider()
    {
        var mock = Mock.Of<IArity7027>(MockBehavior.Loose, new Provider(_ => true, _ => "provided"));
        mock.S2(1, 2).Returns("configured");

        await Assert.That(mock.Object.S2(1, 2)).IsEqualTo("configured");
        await Assert.That(mock.Object.S2(3, 4)).IsEqualTo("provided");
    }

    [Test]
    public async Task Clearing_The_Provider_Restores_Built_In_Defaults()
    {
        var mock = Mock.Of<IArity7027>(MockBehavior.Loose, new Provider(_ => true, _ => "provided"));
        Mock.SetDefaultValueProvider(mock, null);

        await Assert.That(Mock.GetDefaultValueProvider(mock)).IsNull();
        await Assert.That(mock.Object.S1(1)).IsEqualTo("");
    }

    private static IArity7027 CreateWith(IDefaultValueProvider provider)
        => Mock.Of<IArity7027>(MockBehavior.Loose, provider).Object;

    private static string?[] Strings(IArity7027 c) =>
    [
        c.S0(), c.S1(1), c.S2(1, 2), c.S3(1, 2, 3), c.S4(1, 2, 3, 4), c.S5(1, 2, 3, 4, 5),
        c.S6(1, 2, 3, 4, 5, 6), c.S7(1, 2, 3, 4, 5, 6, 7), c.S8(1, 2, 3, 4, 5, 6, 7, 8),
        c.S9(1, 2, 3, 4, 5, 6, 7, 8, 9),
    ];

    private static IChild7027[] Children(IArity7027 c) =>
    [
        c.C0(), c.C1(1), c.C2(1, 2), c.C3(1, 2, 3), c.C4(1, 2, 3, 4), c.C5(1, 2, 3, 4, 5),
        c.C6(1, 2, 3, 4, 5, 6), c.C7(1, 2, 3, 4, 5, 6, 7), c.C8(1, 2, 3, 4, 5, 6, 7, 8),
        c.C9(1, 2, 3, 4, 5, 6, 7, 8, 9),
    ];
}
