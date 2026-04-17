using Verifier = TUnit.AspNetCore.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.AspNetCore.Analyzers.DirectWebApplicationFactoryInheritanceAnalyzer,
    TUnit.AspNetCore.Analyzers.CodeFixers.UseTestWebApplicationFactoryCodeFixProvider>;

namespace TUnit.AspNetCore.Analyzers.Tests;

public class UseTestWebApplicationFactoryCodeFixProviderTests
{
    [Test]
    public async Task Rewrites_Base_Type_To_TestWebApplicationFactory()
    {
        var source = $$"""
            {{WebApplicationFactoryStubs.Source}}

            public class MyFactory : {|#0:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|}
            {
            }
            """;

        var fixedSource = $$"""
            using TUnit.AspNetCore;

            {{WebApplicationFactoryStubs.Source}}

            public class MyFactory : TestWebApplicationFactory<Program>
            {
            }
            """;

        await Verifier.VerifyCodeFixAsync(
            source,
            fixedSource,
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance)
                .WithLocation(0)
                .WithArguments("MyFactory"));
    }
}
