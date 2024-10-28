using NUnit.Framework;
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
}