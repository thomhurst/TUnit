using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.InstanceValuesInTestClassAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class InstanceValuesInTestClassAnalyzerTests
{
    [Test]
    public async Task Flag_When_Assigning_ClassInstance_Data()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    private int _value;
                                
                    [Test]
                    public void MyTest(string value)
                    {
                        {|#0:_value = 99|};
                    }

                }
                """,

                Verifier.Diagnostic(Rules.InstanceAssignmentInTestClass)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Do_Not_Flag_When_Not_Assigning_ClassInstance_Data()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using TUnit.Core;

                public class MyClass
                {
                    private int _value;
                                
                    [Test]
                    public void MyTest(string value)
                    {
                        Console.WriteLine(_value);
                    }

                }
                """
            );
    }

    [Test]
    public async Task Do_Not_Flag_When_Not_Assigning_To_New_Class()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using TUnit.Core;

                public record SomeDataClass
                {
                    public required Guid SomeGuid { get; set; }
                    public required Guid SomeGuid2 { get; set; }
                    public required Guid MyCoolGuid { get; set; }
                }
                
                public class TestTest
                {
                
                    private readonly Guid SomeGuid;
                    private readonly Guid SomeGuid2;
                    private Guid SomeGuid3; // IDE0052 => for this context, ignored
                    private readonly Guid SomeVeryCoolGuid;
                
                    public TestTest()
                    {
                        SomeGuid = Guid.NewGuid();
                        SomeGuid2 = Guid.NewGuid();
                        SomeGuid3 = Guid.NewGuid();
                        SomeVeryCoolGuid = Guid.NewGuid();
                    }
                
                    [Test]
                    public void SomeTest()
                    {
                        var _ = new SomeDataClass()
                        {
                            SomeGuid = SomeGuid, // 2 => Warning: TUnit0018
                            MyCoolGuid = SomeGuid2, // 3 => nothing
                            SomeGuid2 = SomeVeryCoolGuid // 4 => Warning: TUnit0018
                        };
                    }
                }
                """
            );
    }
}
