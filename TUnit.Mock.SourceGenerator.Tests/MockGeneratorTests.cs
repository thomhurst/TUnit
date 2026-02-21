namespace TUnit.Mock.SourceGenerator.Tests;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
            using TUnit.Mock;

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
    public Task Interface_With_Mixed_Members()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            using TUnit.Mock;

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
}
