using TUnit.Analyzers.Tests.Verifiers;

namespace TUnit.Analyzers.Tests;

/// <summary>
/// Tests for the AOT compatibility analyzer
/// </summary>
public class AotCompatibilityAnalyzerTests
{
    [Test]
    public async Task GenericTestClass_WithoutExplicitInstantiation_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class {|TUnit0058:GenericTestClass|}<T>
            {
                [Test]
                public void TestMethod()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericTestClass_WithExplicitInstantiation_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            [GenerateGenericTest(typeof(int))]
            [GenerateGenericTest(typeof(string))]
            public class GenericTestClass<T>
            {
                [Test]
                public void TestMethod()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericTestMethod_WithoutExplicitInstantiation_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void {|TUnit0058:GenericTestMethod|}<T>()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericTestMethod_WithExplicitInstantiation_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [GenerateGenericTest(typeof(int))]
                [GenerateGenericTest(typeof(string))]
                [Test]
                public void GenericTestMethod<T>()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }





    [Test]
    public async Task ClassDataSourceAttribute_WithTypeof_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [ClassDataSource(typeof(DataProvider)), Test]
                public void TestMethod(DataProvider value)
                {
                }
            }

            public class DataProvider
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ClassDataSourceAttribute_WithParamsArray_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [{|TUnit0059:ClassDataSource(new[] { typeof(DataProvider), typeof(DataProvider2) })|}, Test]
                public void TestMethod(object[] values)
                {
                }
            }

            public class DataProvider
            {
                public string Name { get; set; }
            }

            public class DataProvider2
            {
                public int Id { get; set; }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ClassDataSourceAttribute_WithMultipleTypeof_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [ClassDataSource(typeof(DataProvider), typeof(DataProvider2)), Test]
                public void TestMethod(DataProvider value1, DataProvider2 value2)
                {
                }
            }

            public class DataProvider
            {
                public string Name { get; set; }
            }

            public class DataProvider2
            {
                public int Id { get; set; }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task GenericClassDataSourceAttribute_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [ClassDataSource<DataProvider>(), Test]
                public void TestMethod(DataProvider value)
                {
                }
            }

            public class DataProvider
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task MethodDataSourceAttribute_ShowsError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [{|TUnit0059:MethodDataSource(nameof(GetData))|}, Test]
                public void TestMethod(int value)
                {
                }

                public static IEnumerable<int> GetData() => [1, 2, 3];
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ArgumentsAttribute_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Arguments(1, 2, 3), Test]
                public void TestMethod(int value)
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task NonGenericTestClass_NoError()
    {
        var source = """
            using TUnit.Core;

            namespace TestProject;

            public class SimpleTestClass
            {
                [Test]
                public void TestMethod()
                {
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    // Enhanced AOT Compatibility Tests

    [Test]
    public async Task ExpressionCompile_ShowsError()
    {
        var source = """
            using System;
            using System.Linq.Expressions;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    Expression<Func<int, bool>> expr = x => x > 5;
                    var {|TUnit0061:compiled|} = expr.Compile();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task TypeGetType_WithStringLiteral_ShowsError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var type = {|TUnit0063:Type.GetType("System.String")|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task MakeGenericType_ShowsError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var listType = typeof(System.Collections.Generic.List<>);
                    var stringListType = {|TUnit0064:listType.MakeGenericType(typeof(string))|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task MakeArrayType_ShowsError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var stringType = typeof(string);
                    var arrayType = {|TUnit0064:stringType.MakeArrayType()|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ActivatorCreateInstance_ShowsError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var instance = {|TUnit0065:Activator.CreateInstance(typeof(string))|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ReflectionMethods_ShowError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var type = typeof(string);
                    var methods = {|TUnit0062:type.GetMethods()|};
                    var properties = {|TUnit0062:type.GetProperties()|};
                    var fields = {|TUnit0062:type.GetFields()|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task ReflectionEmit_ShowsError()
    {
        var source = """
            using System;
            using System.Reflection;
            using System.Reflection.Emit;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var assemblyName = new AssemblyName("TestAssembly");
                    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                    var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");
                    var typeBuilder = {|TUnit0067:moduleBuilder.DefineType("TestType")|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task AssemblyLoadFrom_ShowsWarning()
    {
        var source = """
            using System;
            using System.Reflection;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var assembly = {|TUnit0068:Assembly.LoadFrom("test.dll")|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task DynamicCodeGeneration_ShowsError()
    {
        var source = """
            using System;
            using Microsoft.CSharp;
            using System.CodeDom.Compiler;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var provider = new CSharpCodeProvider();
                    var results = {|TUnit0066:provider.CompileAssemblyFromSource(new CompilerParameters(), "public class Test {}")|};
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task TypeOf_NoError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var type = typeof(string); // This is AOT-compatible
                    var name = type.Name;
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Test]
    public async Task NonReflectionMethods_NoError()
    {
        var source = """
            using System;
            using TUnit.Core;

            namespace TestProject;

            public class TestClass
            {
                [Test]
                public void TestMethod()
                {
                    var str = "Hello";
                    var length = str.Length; // Not a reflection method
                    var upper = str.ToUpper();
                }
            }
            """;

        await CSharpAnalyzerVerifier<AotCompatibilityAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
