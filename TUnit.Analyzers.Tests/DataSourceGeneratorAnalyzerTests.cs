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
                [WrongTypeGenerator<int>]
                public class TypeMismatchTest(int {|TUnit0001:value|})
                {
                    [Test]
                    public void SomeTest()
                    {
                    }
                }
                """
            );
    }
}
