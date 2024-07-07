using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DisposableFieldPropertyAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DisposableFieldPropertyAnalyzerTests
{
    [Test]
    public async Task DataDriven_Argument_Is_Flagged_When_No_Parameters_Passed()
    {
        const string text = """
                            using System.Net.Http;
                            using TUnit.Core;

                            public class DisposableFieldTests
                            {
                                {|#0:private HttpClient _httpClient;|}
                            
                                [BeforeEachTest]
                                public void Setup()
                                {
                                    _httpClient = new HttpClient();
                                }
                            
                                [Test]
                                public void Test1()
                                {
                                }
                            }
                            """;

        var expected = Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected).ConfigureAwait(false);
    }
    
    [Test]
    public async Task Not_Flagged_When_InjectedClassData()
    {
        const string text = """
                            using System.Net.Http;
                            using TUnit.Core;

                            [ClassDataSource(typeof(HttpClient), Shared = SharedType.Keyed, Key = "🌲")]
                            public class DisposableFieldTests
                            {
                                private HttpClient _httpClient;
                            
                                public DisposableFieldTests(HttpClient httpClient)
                                {
                                    _httpClient = httpClient;
                                }
                            
                                [Test]
                                public void Test1()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}