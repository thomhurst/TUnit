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

    [Test]
    [Arguments("Class", "Assembly")]
    [Arguments("Class", "TestSession")]
    [Arguments("Assembly", "Class")]
    [Arguments("Assembly", "TestSession")]
    [Arguments("TestSession", "Class")]
    [Arguments("TestSession", "Assembly")]
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

    [Test]
    [Arguments("Class")]
    [Arguments("Assembly")]
    [Arguments("TestSession")]
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

    [Test]
    public async Task Not_Flagged_When_IDisposable_Base_Class_And_No_Disposable_Created()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public abstract class DisposedReproTestBase : IDisposable
                {
                    private bool _disposed;

                    public void CheckDisposed()
                    {
                        if (_disposed)
                        {
                            throw new InvalidOperationException("Already disposed");
                        }
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                    }
                }

                public sealed record Dummy2;

                [ClassDataSource<Dummy2>]
                public sealed class DisposedRepro : DisposedReproTestBase
                {
                    private readonly Dummy2 _dummy;

                    public DisposedRepro(Dummy2 dummy)
                    {
                        _dummy = dummy;
                    }

                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        CheckDisposed();
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """
            );
    }

    [Test]
    public async Task Bug_Reproduction_DisposableInjectedParameter_Should_Not_Be_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public abstract class DisposableTestBase : IDisposable
                {
                    private bool _disposed;

                    public void CheckDisposed()
                    {
                        if (_disposed)
                        {
                            throw new InvalidOperationException("Already disposed");
                        }
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                    }
                }

                [ClassDataSource<HttpClient>]
                public sealed class DisposableInjectedTest : DisposableTestBase
                {
                    public DisposableInjectedTest(HttpClient httpClient)
                    {
                        // HttpClient is injected via ClassDataSource, not created by us
                    }

                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        CheckDisposed();
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """
            );
    }

    [Test]
    public async Task Bug_Reproduction_Primary_Constructor_With_Disposable_Parameter_Should_Not_Be_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public abstract class DisposedReproTestBase : IDisposable
                {
                    private bool _disposed;

                    public void CheckDisposed()
                    {
                        if (_disposed)
                        {
                            throw new InvalidOperationException("Already disposed");
                        }
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                    }
                }

                [ClassDataSource<HttpClient>]
                public sealed class DisposedRepro(HttpClient httpClient) : DisposedReproTestBase
                {
                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        CheckDisposed();
                        // httpClient is available here via primary constructor parameter
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """
            );
    }

    [Test]
    public async Task Bug_Reproduction_Should_Flag_When_Creating_Disposable_In_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public abstract class DisposedReproTestBase : IDisposable
                {
                    private bool _disposed;

                    public void CheckDisposed()
                    {
                        if (_disposed)
                        {
                            throw new InvalidOperationException("Already disposed");
                        }
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                    }
                }

                public sealed class DisposedRepro : DisposedReproTestBase
                {
                    private HttpClient {|#0:_httpClient|};

                    public DisposedRepro()
                    {
                        _httpClient = new HttpClient();
                    }

                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        CheckDisposed();
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """,
                
                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_httpClient")
            );
    }

    [Test]
    public async Task Primary_Constructor_Parameter_Captured_As_Field_Should_Not_Be_Flagged_When_Not_Creating_New_Object()
    {
        // This test checks the case where a primary constructor parameter of disposable type
        // is captured as an implicit field, but should not be flagged because we're not creating the object
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public abstract class DisposedReproTestBase : IDisposable
                {
                    private bool _disposed;

                    public void CheckDisposed()
                    {
                        if (_disposed)
                        {
                            throw new InvalidOperationException("Already disposed");
                        }
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                    }
                }

                [ClassDataSource<HttpClient>]
                public sealed class DisposedRepro(HttpClient httpClient) : DisposedReproTestBase
                {
                    // Here httpClient becomes an implicit field/property, but we're not creating it
                    // It's injected via ClassDataSource, so analyzer should not flag it
                    
                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        CheckDisposed();
                        var result = httpClient.BaseAddress; // Use the injected client
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """
            );
    }

    [Test]
    public async Task Bug_Scenario_Disposable_Field_In_IDisposable_Class_Should_Be_Flagged_If_Created_And_Not_Disposed()
    {
        // This might be the actual issue: when a class implements IDisposable but has a field
        // that's created but not properly disposed in the Dispose method
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public sealed class DisposedRepro : IDisposable
                {
                    private HttpClient {|#0:_httpClient|};
                    private bool _disposed;

                    public DisposedRepro()
                    {
                        _httpClient = new HttpClient();
                    }

                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        // Test code
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                        // BUG: _httpClient is not disposed here, so analyzer should flag it
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """,
                
                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_httpClient")
            );
    }

    [Test]
    public async Task Bug_Reproduction_False_Positive_Primary_Constructor_Parameter_Flagged_Incorrectly()
    {
        // This test attempts to reproduce a false positive where a primary constructor parameter
        // is incorrectly flagged as needing disposal, even though it's not created by the class
        
        // FIRST: Let's establish a scenario that SHOULD be flagged (create and don't dispose)
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public abstract class TestBase : IDisposable
                {
                    private bool _disposed;
                    public void Dispose() => _disposed = true;
                }

                public sealed class TestClass : TestBase
                {
                    private HttpClient {|#0:_client|};
                    
                    public TestClass()
                    {
                        _client = new HttpClient(); // Creating object - should be flagged
                    }

                    [Test]
                    public void TestMethod() { }
                }
                """,
                
                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_client")
            );
    }

    [Test]
    public async Task Bug_Reproduction_Check_Primary_Constructor_Parameter_Should_Not_Be_Flagged()
    {
        // This test verifies primary constructor parameters of disposable types should NOT be flagged
        // when they are not created by the class (injected via dependency injection or data source)
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public abstract class TestBase : IDisposable
                {
                    private bool _disposed;
                    public void Dispose() => _disposed = true;
                }

                [ClassDataSource<HttpClient>]
                public sealed class TestClass(HttpClient client) : TestBase
                {
                    // Primary constructor parameter 'client' is implicitly captured as a field
                    // But we're NOT creating it - it's injected, so analyzer should NOT flag it
                    
                    [Test]
                    public void TestMethod() 
                    {
                        var result = client.BaseAddress; // Using the injected client
                    }
                }
                """
            );
    }

    [Test]
    public async Task Primary_Constructor_Parameter_With_Assignment_Should_Be_Flagged_If_Creating_New_Object()
    {
        // Test to ensure that if we DO create a new disposable object and assign it to a field,
        // it SHOULD be flagged
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Net.Http;
                using TUnit.Core;

                public sealed class TestClass : IDisposable
                {
                    private HttpClient {|#0:_anotherClient|};
                    
                    public TestClass()
                    {
                        // This is creating a new object and assigning to field - SHOULD be flagged
                        _anotherClient = new HttpClient();
                    }
                    
                    [Test]
                    public void TestMethod() 
                    {
                        // Test method
                    }
                    
                    public void Dispose() 
                    {
                        // Not disposing _anotherClient - should be flagged
                    }
                }
                """,
                
                Verifier.Diagnostic(Rules.Dispose_Member_In_Cleanup)
                    .WithLocation(0)
                    .WithArguments("_anotherClient")
            );
    }

    [Test]
    public async Task Exact_DisposedRepro_Scenario_Should_Not_Be_Flagged()
    {
        // This replicates the exact scenario from DisposedRepro.cs
        // The analyzer should NOT flag anything in this scenario
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public abstract class DisposedReproTestBase : IDisposable
                {
                    private bool _disposed;

                    public void CheckDisposed()
                    {
                        if (_disposed)
                        {
                            throw new InvalidOperationException("Already disposed");
                        }
                    }

                    public void Dispose()
                    {
                        _disposed = true;
                    }
                }

                public sealed record Dummy2;

                [ClassDataSource<Dummy2>]
                public sealed class DisposedRepro(Dummy2 dummy) : DisposedReproTestBase
                {
                    [Test]
                    [MethodDataSource(nameof(GetValues))]
                    public void DoTest(int value)
                    {
                        CheckDisposed();
                    }

                    public static IEnumerable<int> GetValues() => new[] { 1, 2, 3 };
                }
                """
            );
    }
}
