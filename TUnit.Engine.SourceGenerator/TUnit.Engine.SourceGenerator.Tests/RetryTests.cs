using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace TUnit.Engine.SourceGenerator.Tests;

public class RetryTests : TestsBase
{
    [Fact]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "RetryTests.cs"),
        generatedFiles =>
        {
            Assert.Equal(3, generatedFiles.Length);

            Assert.Contains("RetryCount = 1,", generatedFiles[0]);
            Assert.Contains("RetryCount = 2,", generatedFiles[1]);
            Assert.Contains("RetryCount = 3,", generatedFiles[2]);
        });
}