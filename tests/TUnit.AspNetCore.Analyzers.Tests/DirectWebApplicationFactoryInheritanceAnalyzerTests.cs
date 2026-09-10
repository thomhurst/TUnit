using Verifier = TUnit.AspNetCore.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.AspNetCore.Analyzers.DirectWebApplicationFactoryInheritanceAnalyzer>;

namespace TUnit.AspNetCore.Analyzers.Tests;

public class DirectWebApplicationFactoryInheritanceAnalyzerTests
{
    [Test]
    public async Task Warning_When_Direct_WebApplicationFactory_Inheritance()
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{WebApplicationFactoryStubs.Source}}

            public class MyFactory : {|#0:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|}
            {
            }
            """,
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance)
                .WithLocation(0)
                .WithArguments("MyFactory"));
    }

    [Test]
    public async Task No_Warning_When_Using_TestWebApplicationFactory()
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{WebApplicationFactoryStubs.Source}}

            public class MyFactory : TUnit.AspNetCore.TestWebApplicationFactory<Program>
            {
            }
            """);
    }

    [Test]
    public async Task No_Warning_When_Transitively_Inherits_Via_TestWebApplicationFactory()
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{WebApplicationFactoryStubs.Source}}

            public class BaseFactory : TUnit.AspNetCore.TestWebApplicationFactory<Program>
            {
            }

            public class MyFactory : BaseFactory
            {
            }
            """);
    }

    [Test]
    public async Task Warning_Fires_Once_For_Partial_Class()
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{WebApplicationFactoryStubs.Source}}

            public partial class MyFactory : {|#0:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|}
            {
            }

            public partial class MyFactory
            {
            }
            """,
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance)
                .WithLocation(0)
                .WithArguments("MyFactory"));
    }

    [Test]
    public async Task Warning_On_Base_Type_Location()
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            {{WebApplicationFactoryStubs.Source}}

            public class A : {|#0:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|} { }
            public class B : {|#1:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|} { }
            """,
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance).WithLocation(0).WithArguments("A"),
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance).WithLocation(1).WithArguments("B"));
    }
}
