using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ApplicableAttributeTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ApplicableAttributeTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "CustomSkipAttribute.cs"),
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "SomethingElseAttribute.cs")
            ]
        },
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(1));

            Assert.That(generatedFiles[0], Does.Contain("ApplicableTestAttributes = [..methodInfo.GetCustomAttributes<global::TUnit.TestProject.CustomSkipAttribute>(), ..classType.GetCustomAttributes<global::TUnit.TestProject.CustomSkipAttribute>(), ..methodInfo.GetCustomAttributes<global::TUnit.TestProject.SomethingElseAttribute>(), ..typeof(global::TUnit.TestProject.ApplicableAttributeTests).GetCustomAttributes<global::TUnit.TestProject.SomethingElseAttribute>()],"));
        });
}
