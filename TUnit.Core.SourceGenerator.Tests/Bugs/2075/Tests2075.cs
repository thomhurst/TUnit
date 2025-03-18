using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests.Bugs._2075;

internal class Tests2075 : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2075",
            "Tests.cs"),
        new RunTestOptions
        {
#if NET9_0_OR_GREATER
            AdditionalSyntaxes = 
            [
                "#define NET9_0_OR_GREATER",
            ],
#endif
            VerifyConfigurator = task => task.UniqueForTargetFrameworkAndVersion()
        },
         _ => Task.CompletedTask);
}