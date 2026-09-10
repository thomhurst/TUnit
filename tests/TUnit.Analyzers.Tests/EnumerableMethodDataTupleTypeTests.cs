using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class EnumerableMethodDataTupleTypeTests
{
    [Test]
    public async Task Invalid_Enumerable_Tuple_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using System;
                using TUnit.Core;
                            
                public class EnumerableTupleDataSourceDrivenTests
                {
                    [Test]
                    [{|#0:MethodDataSource(nameof(TupleMethod))|}]
                    public void DataSource_TupleMethod(int value, string value2, bool value3, int va4)
                    {
                    }
                                
                    public static IEnumerable<Func<(int, string, bool)>> TupleMethod()
                    {
                        yield return () => (1, "String", true);
                        yield return () => (2, "String2", false);
                        yield return () => (3, "String3", true);
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithArguments("int, string, bool", "int, string, bool, int")
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Test_Method_With_Tuple_Parameter_Should_Raise_Error()
    {
        // This tests the new functionality - tuple parameters with tuple data sources should error
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using System;
                using TUnit.Core;
                            
                public class EnumerableTupleDataSourceDrivenTests
                {
                    [Test]
                    [{|#0:MethodDataSource(nameof(TupleMethod))|}]
                    public void DataSource_TupleMethod((int value, string value2) tupleParam)
                    {
                    }
                                
                    public static IEnumerable<Func<(int, string)>> TupleMethod()
                    {
                        yield return () => (1, "String");
                        yield return () => (2, "String2");
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithArguments("int, string", "(int value, string value2)")
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Valid_Enumerable_Tuple_Raises_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using System;
                using TUnit.Core;

                public class EnumerableTupleDataSourceDrivenTests
                {
                    [Test]
                    [{|#0:MethodDataSource(nameof(TupleMethod))|}]
                    public void DataSource_TupleMethod(int value, string value2, bool value3)
                    {
                    }
                                
                    public static IEnumerable<Func<(int, string, bool)>> TupleMethod()
                    {
                        yield return () => (1, "String", true);
                        yield return () => (2, "String2", false);
                        yield return () => (3, "String3", true);
                    }
                }
                """
            );
    }
}
