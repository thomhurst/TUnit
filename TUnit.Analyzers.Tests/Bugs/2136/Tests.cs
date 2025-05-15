using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests.Bugs._2136;

public class Tests
{
    [Test]
    public async Task Valid_Generic_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                            
                public class Tests
                {
                    [Test]
                    [Arguments(true, "True")]
                    [Arguments(1, "1")]
                    [Arguments(1.1, "1.1")]
                    [Arguments("hello", "hello")]
                    [Arguments(MyEnum.Item, "Item")]
                    public void GenericArgumentsTest<T>(T value, string expected)
                    {
                    }
                }
                
                public enum MyEnum
                {
                    Item,
                    Item2
                }
                """
            );
    }
}