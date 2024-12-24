using System.Globalization;
using TUnit.Assertions.Extensions;
using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NumberArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NumberArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(6);
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 1;");
            await AssertFileContains(generatedFiles[1], "global::System.Double methodArg = 1.1;");
            await AssertFileContains(generatedFiles[2], "global::System.Single methodArg = 1.1f;");
            await AssertFileContains(generatedFiles[3], "global::System.Int64 methodArg = 1L;");
            await AssertFileContains(generatedFiles[4], "global::System.UInt64 methodArg = 1UL;");
            await AssertFileContains(generatedFiles[5], "global::System.UInt32 methodArg = 1U;");
        });

    [Test]
    [TestExecutor<SetCulture>]
    public Task TestDE() => Test();

    public class SetCulture : GenericAbstractExecutor
    {
        protected override async Task ExecuteAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<object?>();
        
            var thread = new Thread(() =>
            {
                try
                {
                    action().GetAwaiter().GetResult();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
        
            var cultureInfoByIetfLanguageTag = CultureInfo.GetCultureInfoByIetfLanguageTag("de-DE");
            thread.CurrentCulture = cultureInfoByIetfLanguageTag;
            thread.CurrentUICulture = cultureInfoByIetfLanguageTag;
            thread.Start();
        
            await tcs.Task;
        }

        protected override void ExecuteSync(Action action)
        {
            var thread = new Thread(() => action());

            var cultureInfoByIetfLanguageTag = CultureInfo.GetCultureInfoByIetfLanguageTag("de-DE");
            thread.CurrentCulture = cultureInfoByIetfLanguageTag;
            thread.CurrentUICulture = cultureInfoByIetfLanguageTag;
            thread.Start();
            thread.Join();
        }
    }
}