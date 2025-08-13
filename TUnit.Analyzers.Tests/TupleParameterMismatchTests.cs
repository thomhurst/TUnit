using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests
{
    public class TupleParameterMismatchTests
    {
        [Test]
        public async Task Test_Method_With_Tuple_Parameter_And_Tuple_DataSource_Should_Raise_Error()
        {
            // This is the problematic case from the issue
            // Data source returns Func<(int, int)> which gets unpacked to 2 arguments
            // But test method expects single tuple parameter - this should error
            await Verifier
                .VerifyAnalyzerAsync(
                    """
                    using System.Collections.Generic;
                    using System;
                    using TUnit.Core;

                    public class TupleParameterTests
                    {
                        [Test]
                        [{|#0:MethodDataSource(nameof(IncreasingLoad))|}]
                        public void CanHandleManyRequests_With_Changing_Subscribers((int consumers, int requests) state)
                        {
                        }

                        public static IEnumerable<Func<(int consumers, int messages)>> IncreasingLoad()
                        {
                            yield return () => (1, 10);
                            yield return () => (5, 50);
                        }
                    }
                    """,

                    Verifier
                        .Diagnostic(Rules.WrongArgumentTypeTestData)
                        .WithArguments("int, int", "(int consumers, int requests)")
                        .WithLocation(0)
                );
        }

        [Test]
        public async Task Test_Method_With_Separate_Parameters_And_Tuple_DataSource_Should_Not_Raise_Error()
        {
            // This is the correct way - data source returns tuples, method accepts separate parameters
            await Verifier
                .VerifyAnalyzerAsync(
                    """
                    using System.Collections.Generic;
                    using System;
                    using TUnit.Core;

                    public class TupleParameterTests
                    {
                        [Test]
                        [MethodDataSource(nameof(IncreasingLoad))]
                        public void CanHandleManyRequests_With_Separate_Parameters(int consumers, int requests)
                        {
                        }

                        public static IEnumerable<Func<(int consumers, int messages)>> IncreasingLoad()
                        {
                            yield return () => (1, 10);
                            yield return () => (5, 50);
                        }
                    }
                    """
                );
        }
    }
}