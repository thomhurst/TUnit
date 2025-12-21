using AspNetCoreRules = TUnit.AspNetCore.Analyzers.Rules;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.AspNetCore.Analyzers.WebApplicationFactoryAccessAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class WebApplicationFactoryAccessAnalyzerTests
{
    private const string WebApplicationTestStub = """
        namespace TUnit.AspNetCore
        {
            public abstract class WebApplicationTest
            {
                public int UniqueId { get; }
            }

            public abstract class WebApplicationTest<TFactory, TEntryPoint> : WebApplicationTest
                where TFactory : class, new()
                where TEntryPoint : class
            {
                public TFactory GlobalFactory { get; set; } = null!;
                public object Factory { get; } = null!;
                public System.IServiceProvider Services { get; } = null!;
                public object? HttpCapture { get; }

                protected virtual System.Threading.Tasks.Task SetupAsync() => System.Threading.Tasks.Task.CompletedTask;
            }
        }
        """;

    [Test]
    public async Task No_Error_When_Accessing_Factory_In_Test_Method()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    [Test]
                    public void MyTest()
                    {
                        var factory = Factory;
                        var services = Services;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Error_When_Accessing_Factory_In_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    public MyTests()
                    {
                        var factory = {|#0:Factory|};
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(AspNetCoreRules.FactoryAccessedTooEarly)
                    .WithLocation(0)
                    .WithArguments("Factory", "constructor")
            );
    }

    [Test]
    public async Task Error_When_Accessing_Services_In_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    public MyTests()
                    {
                        var services = {|#0:Services|};
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(AspNetCoreRules.FactoryAccessedTooEarly)
                    .WithLocation(0)
                    .WithArguments("Services", "constructor")
            );
    }

    [Test]
    public async Task Error_When_Accessing_Factory_In_SetupAsync()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using System.Threading.Tasks;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    protected override Task SetupAsync()
                    {
                        var factory = {|#0:Factory|};
                        return Task.CompletedTask;
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(AspNetCoreRules.FactoryAccessedTooEarly)
                    .WithLocation(0)
                    .WithArguments("Factory", "SetupAsync")
            );
    }

    [Test]
    public async Task Error_When_Accessing_HttpCapture_In_SetupAsync()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using System.Threading.Tasks;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    protected override Task SetupAsync()
                    {
                        var capture = {|#0:HttpCapture|};
                        return Task.CompletedTask;
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(AspNetCoreRules.FactoryAccessedTooEarly)
                    .WithLocation(0)
                    .WithArguments("HttpCapture", "SetupAsync")
            );
    }

    [Test]
    public async Task Error_When_Accessing_GlobalFactory_In_Constructor()
    {
        // GlobalFactory is NOT available in constructor - it's injected via property injection after construction
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    public MyTests()
                    {
                        var factory = {|#0:GlobalFactory|};
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """,
                Verifier.Diagnostic(AspNetCoreRules.FactoryAccessedTooEarly)
                    .WithLocation(0)
                    .WithArguments("GlobalFactory", "constructor")
            );
    }

    [Test]
    public async Task No_Error_When_Accessing_GlobalFactory_In_SetupAsync()
    {
        // GlobalFactory IS available in SetupAsync - it's injected before SetupAsync runs
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using System.Threading.Tasks;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    protected override Task SetupAsync()
                    {
                        var factory = GlobalFactory;
                        return Task.CompletedTask;
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Accessing_UniqueId_In_SetupAsync()
    {
        // UniqueId IS available in SetupAsync
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using System.Threading.Tasks;
                {{WebApplicationTestStub}}

                public class MyFactory { }
                public class Program { }

                public class MyTests : TUnit.AspNetCore.WebApplicationTest<MyFactory, Program>
                {
                    protected override Task SetupAsync()
                    {
                        var id = UniqueId;
                        return Task.CompletedTask;
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_For_Unrelated_Factory_Property()
    {
        // A property named Factory on an unrelated class should not trigger the analyzer
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class SomeClass
                {
                    public object Factory { get; } = null!;
                }

                public class MyTests
                {
                    private SomeClass _someClass = new();

                    public MyTests()
                    {
                        var factory = _someClass.Factory;
                    }

                    [Test]
                    public void MyTest()
                    {
                    }
                }
                """
            );
    }
}
