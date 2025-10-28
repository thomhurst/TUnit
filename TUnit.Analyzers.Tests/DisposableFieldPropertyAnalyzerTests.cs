﻿using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.DisposableFieldPropertyAnalyzer>;

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
    [Arguments("Class")]
    [Arguments("Assembly")]
    [Arguments("TestSession")]
    public async Task Bug3213(string hook)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                  using System;
                  using System.Linq;
                  using System.Net;
                  using System.Net.Http;
                  using System.Threading;
                  using System.Threading.Tasks;
                  using TUnit.Core;

                  record RegisterPaymentHttp(string BookingId, string RoomId, decimal Amount, DateTimeOffset PaidAt);
                  record BookingState;
                  class Result<T> { public class Ok { public HttpStatusCode StatusCode { get; set; } } }
                  class BookingEvents { public record BookingFullyPaid(DateTimeOffset PaidAt); }
                  class Booking { public object Payload { get; set; } = null!; }
                  class RestRequest {
                      public RestRequest(string path) { }
                      public RestRequest AddJsonBody(object obj) => this;
                  }
                  class ServerFixture {
                      public HttpClient GetClient() => null!;
                      public static BookRoom GetBookRoom() => null!;
                      public Task<System.Collections.Generic.IEnumerable<Booking>> ReadStream<T>(string id) => Task.FromResult(Enumerable.Empty<Booking>());
                  }
                  class BookRoom {
                      public string BookingId => string.Empty;
                      public string RoomId => string.Empty;
                  }
                  class TestEventListener : IDisposable {
                      public void Dispose() { }
                  }
                  static class HttpClientExtensions {
                      public static Task PostJsonAsync(this HttpClient client, string path, object body, CancellationToken cancellationToken) => Task.CompletedTask;
                      public static Task<TResult> ExecutePostAsync<TResult>(this HttpClient client, RestRequest request, CancellationToken cancellationToken) => Task.FromResult<TResult>(default!);
                  }
                  static class ObjectExtensions {
                      public static void ShouldBe(this object obj, object expected) { }
                      public static void ShouldBeEquivalentTo(this object obj, object expected) { }
                  }

                  [ClassDataSource<string>]
                  public class ControllerTests {
                      readonly ServerFixture _fixture = null!;

                      public ControllerTests(string value) {
                      }
                  
                      [Test]
                      public async Task RecordPaymentUsingMappedCommand(CancellationToken cancellationToken) {
                          using var client = _fixture.GetClient();
                  
                          var bookRoom = ServerFixture.GetBookRoom();
                  
                          await client.PostJsonAsync("/book", bookRoom, cancellationToken: cancellationToken);
                  
                          var registerPayment = new RegisterPaymentHttp(bookRoom.BookingId, bookRoom.RoomId, 100, DateTimeOffset.Now);
                  
                          var request  = new RestRequest("/v2/pay").AddJsonBody(registerPayment);
                          var response = await client.ExecutePostAsync<Result<BookingState>.Ok>(request, cancellationToken: cancellationToken);
                          response.StatusCode.ShouldBe(HttpStatusCode.OK);
                  
                          var expected = new BookingEvents.BookingFullyPaid(registerPayment.PaidAt);
                  
                          var events = await _fixture.ReadStream<Booking>(bookRoom.BookingId);
                          var last   = events.LastOrDefault();
                          last!.Payload.ShouldBeEquivalentTo(expected);
                      }
                  
                      static TestEventListener? listener;

                      [After(HookType.{{hook}})]
                      public static void Dispose() => listener?.Dispose();

                      [Before(HookType.{{hook}})]
                      public static void BeforeClass() => listener = new();
                  }
                  """
            );
    }
}
