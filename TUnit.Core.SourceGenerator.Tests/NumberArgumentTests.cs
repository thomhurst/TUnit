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