using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.GlobalTestHooksAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GlobalTestHooksAnalyzerTests
{
    [TestCase("Test", "TestContext")]
    [TestCase("Class", "ClassHookContext")]
    [TestCase("Assembly", "AssemblyHookContext")]
    public async Task Before_No_Error(string hookType, string classType)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using static TUnit.Core.HookType;
                            
                public class Tests
                {
                    [BeforeEvery({{hookType}})]
                    public static void SetUp({{classType}} context)
                    {
                    }
                }
                """
            );
    }
    
    [TestCase("Test", "TestContext")]
    [TestCase("Class", "ClassHookContext")]
    [TestCase("Assembly", "AssemblyHookContext")]
    public async Task After_No_Error(string hookType, string classType)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using TUnit.Core;
                using static TUnit.Core.HookType;
                     
                public class Tests
                {
                    [AfterEvery({{hookType}})]
                    public static void CleanUp({{classType}} context)
                    {
                    }
                }
                """
            );
    }
}