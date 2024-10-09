using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.GeneratedDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DataSourceGeneratorAnalyzerTests
{
    [Test]
    public async Task Constructor_Derived_No_Error_Generic()
    {
        const string text = """
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
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}