// using System.Threading.Tasks;
// using Xunit;
// using Verifier =
//     Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<TUnit.Analyzers.MixAndOrOperatorsAnalyzer,
//         TUnit.Analyzers.SampleCodeFixProvider>;
//
// namespace TUnit.Analyzers.Tests;
//
// public class SampleCodeFixProviderTests
// {
//     [Test]
//     public async Task ClassWithMyCompanyTitle_ReplaceWithCommonKeyword()
//     {
//         const string text = @"
// public class MyCompanyClass
// {
// }
// ";
//
//         const string newText = @"
// public class CommonClass
// {
// }
// ";
//
//         var expected = Verifier.Diagnostic()
//             .WithLocation(2, 14)
//             .WithArguments("MyCompanyClass");
//         await Verifier.VerifyCodeFixAsync(text, expected, newText).ConfigureAwait(false);
//     }
// }