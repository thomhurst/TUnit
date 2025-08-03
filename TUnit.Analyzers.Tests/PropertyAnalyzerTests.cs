using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.PropertyAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class PropertyAnalyzerTests
{
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Only_1_Data_Attribute()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<OtherClass>]
                    public required OtherClass Property { get; init; }
                    
                    [Test]
                    public void MyTest()
                    {
                    }

                }
                
                public class OtherClass;
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_More_Than_1_DataAttribute()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<OtherClass>]
                    [ClassDataSource<OtherClass>]
                    public required OtherClass {|#0:Property|} { get; init; }
                    
                    [Test]
                    public void MyTest()
                    {
                    }

                }

                public class OtherClass;
                """,

                Verifier
                    .Diagnostic(Rules.TooManyDataAttributes)
                    .WithLocation(0)
            );
    }
}
