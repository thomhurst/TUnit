using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DisposableFieldPropertyAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class DisposableFieldPropertyAnalyzerTests
{
    [Test]
    public async Task New_Disposable_Flags_Issue()
    {
        await Verifier
			.VerifyAnalyzerAsync(
				"""
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_httpClient")
            );
    }
    
    [Test]
    public async Task New_Disposable__Static_Flags_Issue()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private static HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.Class)]
                    public static void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_httpClient")
            );
    }
    
    [TestCase("Class", "Assembly")]
    [TestCase("Class", "TestSession")]
    [TestCase("Assembly", "Class")]
    [TestCase("Assembly", "TestSession")]
    [TestCase("TestSession", "Class")]
    [TestCase("TestSession", "Assembly")]
    public async Task New_Disposable__Static_Flags_Issue_When_Wrong_Hook_Used(string beforeHook, string afterHook)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private static HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.{{beforeHook}})]
                    public static void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    [After(HookType.{{afterHook}})]
                    public static void Cleanup()
                    {
                        _httpClient?.Dispose();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """,

                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_httpClient")
            );
    }
    
    [TestCase("Class")]
    [TestCase("Assembly")]
    [TestCase("TestSession")]
    public async Task New_Disposable__Static_No_Issue_When_Same_Hook_Used(string hook)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private static HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.{{hook}})]
                    public static void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    [After(HookType.{{hook}})]
                    public static void Cleanup()
                    {
                        _httpClient?.Dispose();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_Disposable_No_Issue_When_Cleaned_Up_Nullable()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    [After(HookType.Test)]
                    public void Cleanup()
                    {
                        _httpClient?.Dispose();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task New_Disposable_No_Issue_When_Cleaned_Up_PrimaryConstructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Net.Http;
                using TUnit.Core;
                
                public class Test() // note the use of primary constructor here
                {
                    private HttpClient? _client = null!;
                
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _client = new HttpClient();
                    }
                
                    [After(HookType.Test)]
                    public void Cleanup()
                    {
                        _client?.Dispose();
                    }
                
                    [Test]
                    public void TestMethod()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task New_Disposable__Static_No_Issue_When_Cleaned_Up_Nullable()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private static HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.Class)]
                    public static void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    [After(HookType.Class)]
                    public static void Cleanup()
                    {
                        _httpClient?.Dispose();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_IAsyncDisposable_No_Issue_When_Cleaned_Up_Nullable()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                
                using System.IO;
                using TUnit.Core;
                using System.Threading.Tasks;

                public class DisposableFieldTests
                {
                    private StringWriter? {|#0:_stringWriter|};
                            
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _stringWriter = new StringWriter();
                    }
                    
                    [After(HookType.Test)]
                    public async Task Cleanup()
                    {
                        await (_stringWriter?.DisposeAsync() ?? ValueTask.CompletedTask);
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_IAsyncDisposable__Static_No_Issue_When_Cleaned_Up_Nullable()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                
                using System.IO;
                using TUnit.Core;
                using System.Threading.Tasks;

                public class DisposableFieldTests
                {
                    private static StringWriter? {|#0:_stringWriter|};
                            
                    [Before(HookType.Class)]
                    public static void Setup()
                    {
                        _stringWriter = new StringWriter();
                    }
                    
                    [After(HookType.Class)]
                    public static async Task Cleanup()
                    {
                        await (_stringWriter?.DisposeAsync() ?? ValueTask.CompletedTask);
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_Disposable_No_Issue_When_Cleaned_Up()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    [After(HookType.Test)]
                    public void Cleanup()
                    {
                        _httpClient?.Dispose();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_Disposable__Static_No_Issue_When_Cleaned_Up()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Net.Http;
                using TUnit.Core;

                public class DisposableFieldTests
                {
                    private static HttpClient? {|#0:_httpClient|};
                            
                    [Before(HookType.Class)]
                    public static void Setup()
                    {
                        _httpClient = new HttpClient();
                    }
                    
                    [After(HookType.Class)]
                    public static void Cleanup()
                    {
                        _httpClient?.Dispose();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_IAsyncDisposable_No_Issue_When_Cleaned_Up()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.IO;
                using TUnit.Core;
                using System.Threading.Tasks;

                public class DisposableFieldTests
                {
                    private StringWriter? {|#0:_stringWriter|};
                            
                    [Before(HookType.Test)]
                    public void Setup()
                    {
                        _stringWriter = new StringWriter();
                    }
                    
                    [After(HookType.Test)]
                    public async Task Cleanup()
                    {
                        await _stringWriter!.DisposeAsync();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task New_IAsyncDisposable__Static_No_Issue_When_Cleaned_Up()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.IO;
                using TUnit.Core;
                using System.Threading.Tasks;

                public class DisposableFieldTests
                {
                    private static StringWriter? {|#0:_stringWriter|};
                            
                    [Before(HookType.Class)]
                    public static void Setup()
                    {
                        _stringWriter = new StringWriter();
                    }
                    
                    [After(HookType.Class)]
                    public static async Task Cleanup()
                    {
                        await _stringWriter!.DisposeAsync();
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Not_Flagged_When_InjectedClassData()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Net.Http;
                using TUnit.Core;

                [ClassDataSource<HttpClient>(Shared = SharedType.Keyed, Key = "key")]
                public class DisposableFieldTests
                {
                    private HttpClient? _httpClient;
                            
                    public DisposableFieldTests(HttpClient httpClient)
                    {
                        _httpClient = httpClient;
                    }
                            
                    [Test]
                    public void Test1()
                    {
                    }
                }
                """
            );
    }
}