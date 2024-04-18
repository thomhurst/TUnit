using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace TUnit.Engine.SourceGenerator.Tests;

public class NotInParallelTests : TestsBase
{
    [Fact]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NotInParallelTests.cs"),
        generatedFiles =>
        {
            Assert.Equal(12, generatedFiles.Length);

            foreach (var generatedFile in generatedFiles)
            {
                Assert.Contains("NotInParallelConstraintKeys = [],", generatedFile);
            }
        });
}