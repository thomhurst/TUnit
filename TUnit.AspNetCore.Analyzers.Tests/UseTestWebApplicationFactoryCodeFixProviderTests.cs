using Verifier = TUnit.AspNetCore.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.AspNetCore.Analyzers.DirectWebApplicationFactoryInheritanceAnalyzer,
    TUnit.AspNetCore.Analyzers.CodeFixers.UseTestWebApplicationFactoryCodeFixProvider>;

namespace TUnit.AspNetCore.Analyzers.Tests;

public class UseTestWebApplicationFactoryCodeFixProviderTests
{
    [Test]
    public async Task Does_Not_Duplicate_Existing_Using()
    {
        var source = $$"""
            using TUnit.AspNetCore;
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

    [Test]
    public async Task Does_Not_Duplicate_Using_When_Imported_Inside_Namespace()
    {
        var source = $$"""
            {{WebApplicationFactoryStubs.Source}}

            namespace App
            {
                using TUnit.AspNetCore;

                public class MyFactory : {|#0:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|}
                {
                }
            }
            """;

        var fixedSource = $$"""
            {{WebApplicationFactoryStubs.Source}}

            namespace App
            {
                using TUnit.AspNetCore;

                public class MyFactory : TestWebApplicationFactory<Program>
                {
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(
            source,
            fixedSource,
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance)
                .WithLocation(0)
                .WithArguments("MyFactory"));
    }

    [Test]
    public async Task Does_Not_Duplicate_Using_In_File_Scoped_Namespace()
    {
        var source = """
            namespace App;

            using TUnit.AspNetCore;

            public class MyFactory : {|#0:Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>|}
            {
            }
            """;

        var fixedSource = """
            namespace App;

            using TUnit.AspNetCore;

            public class MyFactory : TestWebApplicationFactory<Program>
            {
            }
            """;

        await Verifier.VerifyCodeFixAsync(
            source,
            fixedSource,
            stubsSource: WebApplicationFactoryStubs.Source,
            Verifier.Diagnostic(Rules.DirectWebApplicationFactoryInheritance)
                .WithLocation(0)
                .WithArguments("MyFactory"));
    }

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
