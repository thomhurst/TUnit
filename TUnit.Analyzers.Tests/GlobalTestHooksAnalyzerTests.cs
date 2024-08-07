using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.GlobalTestHooksAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GlobalTestHooksAnalyzerTests
{
    [TestCase("EachTest", "TestContext")]
    [TestCase("Class", "ClassHookContext")]
    [TestCase("Assembly", "AssemblyHookContext")]
    public async Task Before_No_Error(string hookType, string classType)
    {
        var text = $$"""
                            using TUnit.Core;
                            using static TUnit.Core.HookType;
                            
                            public class Tests
                            {
                                [GlobalBefore({{hookType}})]
                                public static void SetUp({{classType}} context)
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
    
    [TestCase("EachTest", "TestContext")]
    [TestCase("Class", "ClassHookContext")]
    [TestCase("Assembly", "AssemblyHookContext")]
    public async Task After_No_Error(string hookType, string classType)
    {
        var text = $$"""
                     using TUnit.Core;
                     using static TUnit.Core.HookType;
                     
                     public class Tests
                     {
                         [GlobalAfter({{hookType}})]
                         public static void CleanUp({{classType}} context)
                         {
                         }
                     }
                     """;
        
        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}