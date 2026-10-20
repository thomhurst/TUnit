using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DataSourceGeneratorAnalyzerTests
{
    [Test]
    public async Task Constructor_Derived_No_Error_Generic()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using TUnit.Core;

                namespace TUnit;

                [ClassDataSource<MyModel>]
                public class MyClass(BaseModel value)
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public record MyModel : BaseModel;
                public record BaseModel;
                """
            );
    }

    [Test]
    public async Task Custom_DataSourceGenerator_With_Wrapper_Type_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                namespace TUnit;

                // This reproduces the issue from GitHub issue #2801
                // Custom data source generator that wraps the target type
                public sealed class ApplicationFixtureGeneratorAttribute<T> : DataSourceGeneratorAttribute<ApplicationFixture<T>>
                    where T : notnull
                { 
                   protected override IEnumerable<Func<ApplicationFixture<T>>> GenerateDataSources(
                            DataGeneratorMetadata dataGeneratorMetadata)
                    {
                        yield return () => new ApplicationFixture<T>();
                    }
                }

                public class ApplicationFixture<T>
                {
                    // Some implementation
                }

                public interface IGantryMethods
                {
                    // Some interface
                }

                // This should not produce TUnit0001 error since types match:
                // Attribute produces: ApplicationFixture<IGantryMethods>
                // Constructor expects: ApplicationFixture<IGantryMethods>
                [ApplicationFixtureGenerator<IGantryMethods>]
                public class MethodCallingTest(ApplicationFixture<IGantryMethods> appFixture)
                {
                    [Test]
                    public void SomeTest()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Custom_DataSourceGenerator_With_Type_Mismatch_Shows_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                namespace TUnit;

                // Custom data source generator that produces different type than expected
                public sealed class WrongTypeGeneratorAttribute<T> : DataSourceGeneratorAttribute<string>
                { 
                   protected override IEnumerable<Func<string>> GenerateDataSources(
                            DataGeneratorMetadata dataGeneratorMetadata)
                    {
                        yield return () => "test";
                    }
                }

                // This should produce TUnit0001 error since types don't match:
                // Attribute produces: string
                // Constructor expects: int
                [{|#0:WrongTypeGenerator<int>|}]
                public class TypeMismatchTest(int value)
                {
                    [Test]
                    public void SomeTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("string", "int")
            );
    }

    [Test]
    public async Task Custom_Generic_DataSource_With_IDataSourceAttribute_No_Error()
    {
        // This test reproduces the issue from GitHub issue #4812
        // Custom generic attribute implementing IDataSourceAttribute where the generic parameter
        // doesn't directly match test parameter types, but the implementation returns compatible types
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                namespace TUnit.Core;

                // Custom data source where T doesn't match test parameter types directly
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                public class CustomDataAttribute<T> : Attribute, IDataSourceAttribute
                {
                    public bool SkipIfEmpty { get; set; }

                    public bool DeferEnumeration { get; set; }

                    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
                    {
                        // Returns Foo instances as the test expects, not T
                        yield return () => Task.FromResult<object?[]?>([new Foo()]);
                    }
                }

                public class Foo { }

                public class Tests
                {
                    [Test]
                    [CustomData<string>] // T is string, but method expects Foo - should not error
                    public void TestMethod(Foo data) { }
                }
                """
            );
    }

    private const string TypedStreamSources =
        """
        using System;
        using System.Collections.Generic;
        using System.IO;
        using System.Threading.Tasks;
        using TUnit.Core;

        public class ObjectStreamsAttribute : DataSourceGeneratorAttribute<object>
        {
            protected override IEnumerable<Func<object>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
            {
                yield return () => new MemoryStream();
            }
        }

        public class StringsAttribute : DataSourceGeneratorAttribute<string>
        {
            protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
            {
                yield return () => "a";
            }
        }

        public class MemoryStreamsAttribute : DataSourceGeneratorAttribute<MemoryStream>
        {
            protected override IEnumerable<Func<MemoryStream>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
            {
                yield return () => new MemoryStream();
            }
        }
        """;

    [Test]
    public async Task Typed_Source_With_Broad_Declared_Type_On_Generic_Method_Explains_Declared_Type_Inference()
    {
        // Source-generated mode infers T from the typed source's declared type, not the run-time value (#7116).
        await Verifier
            .VerifyAnalyzerAsync(
                TypedStreamSources +
                """

                public class MyClass
                {
                    [Test]
                    [{|#0:ObjectStreams|}]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;
                }
                """,
                Verifier.Diagnostic(Rules.GenericTypeArgumentInferredFromDeclaredType)
                    .WithLocation(0)
                    .WithArguments("T", "object")
            );
    }

    [Test]
    public async Task Typed_Source_With_Exact_Declared_Type_Violating_Constraint_Is_Left_To_Run_Time()
    {
        // An exact declared type is not a declared-type inference problem; the run-time constraint failure explains it.
        await Verifier
            .VerifyAnalyzerAsync(
                TypedStreamSources +
                """

                public class MyClass
                {
                    [Test]
                    [Strings]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;
                }
                """
            );
    }

    [Test]
    public async Task Typed_Source_Satisfying_Constraint_On_Generic_Method_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                TypedStreamSources +
                """

                public class MyClass
                {
                    [Test]
                    [MemoryStreams]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;
                }
                """
            );
    }

    [Test]
    public async Task Typed_Source_With_Broad_Declared_Type_And_GenerateGenericTest_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                TypedStreamSources +
                """

                public class MyClass
                {
                    [Test]
                    [ObjectStreams]
                    [GenerateGenericTest(typeof(MemoryStream))]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;
                }
                """
            );
    }
}
