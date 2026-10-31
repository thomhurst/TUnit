namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/7299
// The mock call class of a generic method declares the mocked type's type parameters followed by
// the method's, so `IOutbox<T>` inheriting `Invoke<T>()` produced `MockCall<T, T>` (CS0692) and
// setup extensions that redeclared the extension block's `T` (CS9289). Method type parameters that
// collide with the mocked type's are renamed, consistently across signatures and constraints.
public class Issue7299Tests : SnapshotTestBase
{
    private const string Source = """
        using TUnit.Mocks;

        namespace TestNamespace
        {
            public interface IBus { T Invoke<T>(object message); }
            public interface IOutbox<T> : IBus { T Db { get; } }

            public interface IConverter<TResult> { TResult Convert<T>(T value); }
            public interface IStore<T> : IConverter<T> { void Put<TKey>(TKey key, T value) where TKey : notnull; }

            public interface IConstrained<T> { TOther Select<T, TOther>(T value) where TOther : T; }

            public interface ITaken<T> { void Use<T, TMethod>(T first, TMethod second); }

            public interface IParameterNamed<T> { void Invoke<T>(int TMethod, T value); }

            public interface IMethodNamed<T> { void TMethod<T>(T value); }

            public class Usage
            {
                void M()
                {
                    _ = Mock.Of<IOutbox<string>>();
                    _ = Mock.Of<IStore<string>>();
                    _ = Mock.Of<IConstrained<int>>();
                    _ = Mock.Of<ITaken<int>>();
                    _ = Mock.Of<IParameterNamed<int>>();
                    _ = Mock.Of<IMethodNamed<int>>();
                }
            }
        }
        """;

    [Test]
    public Task Colliding_Method_Type_Parameters_Snapshot()
        => VerifyGeneratorOutput(Source);
}
