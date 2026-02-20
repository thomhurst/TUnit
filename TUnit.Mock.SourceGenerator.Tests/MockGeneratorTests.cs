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
}
