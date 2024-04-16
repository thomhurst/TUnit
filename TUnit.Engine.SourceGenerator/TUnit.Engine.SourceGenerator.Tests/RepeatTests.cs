using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace TUnit.Engine.SourceGenerator.Tests;

public class RepeatTests : TestsBase
{
    [Fact]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "RepeatTests.cs"),
        generatedFiles =>
        {
            Assert.Contains("RepeatCount = 1,", generatedFiles[0]);
            Assert.Contains("RepeatCount = 2,", generatedFiles[1]);
            Assert.Contains("RepeatCount = 3,", generatedFiles[2]);
        });
}