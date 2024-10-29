using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MethodDataSourceAnalyzer>;

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
                using TUnit.Core;
                            
                public class EnumerableTupleDataSourceDrivenTests
                {
                    [Test]
                    [{|#0:MethodDataSource(nameof(TupleMethod))|}]
                    public void DataSource_TupleMethod(int value, string value2, bool value3, int va4)
                    {
                    }
                                
                    public static IEnumerable<(int, string, bool)> TupleMethod()
                    {
                        yield return (1, "String", true);
                        yield return (2, "String2", false);
                        yield return (3, "String3", true);
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.WrongArgumentTypeTestDataSource)
                    .WithArguments("int, string, bool", "int, string, bool, int")
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
                using TUnit.Core;

                public class EnumerableTupleDataSourceDrivenTests
                {
                    [Test]
                    [{|#0:MethodDataSource(nameof(TupleMethod))|}]
                    public void DataSource_TupleMethod(int value, string value2, bool value3)
                    {
                    }
                                
                    public static IEnumerable<(int, string, bool)> TupleMethod()
                    {
                        yield return (1, "String", true);
                        yield return (2, "String2", false);
                        yield return (3, "String3", true);
                    }
                }
                """
            );
    }
}