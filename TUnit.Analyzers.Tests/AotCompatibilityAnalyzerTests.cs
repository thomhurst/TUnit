using Microsoft.CodeAnalysis.Testing;
using TUnit.Analyzers.Tests.Verifiers;

namespace TUnit.Analyzers.Tests;

public class AotCompatibilityAnalyzerTests
{
    [Test]
    public async Task Generic_Test_Method_With_DataSource_Triggers_Warning()
    {
        const string source = """
            using TUnit.Core;
            
            public class MyTests
            {
                [Test]
                [Arguments(1)]
                public void {|TUnit0300:GenericTest|}<T>(T value)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task Tuple_Parameter_Triggers_Warning()
    {
        const string source = """
            using TUnit.Core;
            using System;
            
            public class MyTests
            {
                [Test]
                [Arguments(1, "test")]
                public void TupleTest((int, string) {|TUnit0301:tuple|})
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task Regular_Test_No_Warning()
    {
        const string source = """
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

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task Generic_Class_With_Test_Triggers_Warning()
    {
        const string source = """
            using TUnit.Core;
            
            public class MyTests<T>
            {
                [Test]
                [Arguments(1)]
                public void {|TUnit0300:TestMethod|}(int value)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task MakeGenericType_In_Test_Triggers_Warning()
    {
        const string source = """
            using TUnit.Core;
            using System;
            using System.Collections.Generic;
            
            public class MyTests
            {
                [Test]
                public void TestMethod()
                {
                    var genericType = typeof(List<>);
                    var concreteType = {|TUnit0300:genericType.MakeGenericType(typeof(int))|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task Tuple_Reflection_In_Test_Triggers_Warning()
    {
        const string source = """
            using TUnit.Core;
            using System;
            
            public class MyTests
            {
                [Test]
                public void TestMethod()
                {
                    var tupleType = typeof((int, string));
                    var fields = {|TUnit0301:tupleType.GetFields()|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ValueTuple_Parameter_Triggers_Warning()
    {
        const string source = """
            using TUnit.Core;
            using System;
            
            public class MyTests
            {
                [Test]
                [Arguments(5, 10)]
                public void TestMethod(ValueTuple<int, int> {|TUnit0301:values|})
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task Multiple_Tuple_Parameters_Trigger_Multiple_Warnings()
    {
        const string source = """
            using TUnit.Core;
            using System;
            
            public class MyTests
            {
                [Test]
                [Arguments(1, "a", 2, "b")]
                public void TestMethod((int, string) {|TUnit0301:first|}, (int, string) {|TUnit0301:second|})
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task MakeGenericType_Outside_Test_No_Warning()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            
            public class NotATestClass
            {
                public void RegularMethod()
                {
                    var genericType = typeof(List<>);
                    var concreteType = genericType.MakeGenericType(typeof(int));
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }
}