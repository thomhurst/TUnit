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
            Assert.Equal(4, Regex.Matches(generatedFiles[0], @"NotInParallelConstraintKeys = \[\],").Count);
            Assert.Equal(4, Regex.Matches(generatedFiles[1], @"NotInParallelConstraintKeys = \[\],").Count);
            Assert.Equal(4, Regex.Matches(generatedFiles[2], @"NotInParallelConstraintKeys = \[\],").Count);
        });
}