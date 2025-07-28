using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace TUnit.Analyzers.Tests;

[TestFixture]
public class AotCompatibilityAnalyzerTests : AnalyzerTestFixture<AotCompatibilityAnalyzer>
{
    [Test]
    public async Task Generic_Test_Method_Triggers_Warning()
    {
        const string text = """
            using TUnit.Core;
            
            public class MyTests
            {
                [Test]
                [Arguments(1)]
                [Arguments("test")]
                public void GenericTest<T>(T value)
                {
                }
            }
            """;

        await Verify.DiagnosticExists<AotCompatibilityAnalyzer>(
            AotCompatibilityAnalyzer.GenericTypeNotAotCompatible, 
            text, 
            8, 21, "GenericTest");
    }

    [Test]
    public async Task Tuple_Parameter_Triggers_Warning()
    {
        const string text = """
            using TUnit.Core;
            using System;
            
            public class MyTests
            {
                [Test]
                [Arguments(1, "test")]
                public void TupleTest((int, string) tuple)
                {
                }
            }
            """;

        await Verify.DiagnosticExists<AotCompatibilityAnalyzer>(
            AotCompatibilityAnalyzer.TupleNotAotCompatible, 
            text, 
            8, 39, "tuple");
    }

    [Test]
    public async Task Regular_Test_No_Warning()
    {
        const string text = """
            using TUnit.Core;
            
            public class MyTests
            {
                [Test]
                [Arguments(1, "test")]
                public void RegularTest(int number, string text)
                {
                }
            }
            """;

        await Verify.NoDiagnosticExists<AotCompatibilityAnalyzer>(text);
    }
}