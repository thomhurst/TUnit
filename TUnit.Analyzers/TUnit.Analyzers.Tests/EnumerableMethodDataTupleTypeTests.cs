using System.Threading.Tasks;
using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DataSourceDrivenTestArgumentsAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class EnumerableMethodDataTupleTypeTests
{
    [Test]
    public async Task Invalid_Enumerable_Tuple_Raises_Error()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;
                            
                            public class EnumerableTupleDataSourceDrivenTests
                            {
                                [DataSourceDrivenTest]
                                [{|#0:EnumerableMethodDataSource(nameof(TupleMethod), UnfoldTuple = true)|}]
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
                            """;

        var expected = Verifier
            .Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithArguments("(int, string, bool)", "(int, string, bool, int)")
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
}