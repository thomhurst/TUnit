using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedTestSourceLocationTests : TestsBase
{
    [Test]
    public Task InheritedTests_ShouldUseTestMethodLocation_NotInheritsTestsLocation() => 
        RunTest(
            """
            using TUnit.Core;

            namespace TestProject.SourceLocationFix;

            public abstract class BaseTest
            {
                [Test]
                public async Task SimpleTest()
                {
                    await Assert.That(Environment.ProcessorCount).IsGreaterThan(0);
                }
            }

            [InheritsTests]
            public sealed class DerivedTest : BaseTest
            {
                [Test]
                public async Task ExtraTest()
                {
                    await Assert.That(Environment.ProcessorCount).IsGreaterThan(0);
                }
            }
            """, 
            new RunTestOptions(), async generatedFiles =>
        {
            var generatedCode = string.Join(Environment.NewLine, generatedFiles);
            
            // The inherited test should reference line 7 (where [Test] is) not line 14 (where [InheritsTests] is)
            // Look for the DerivedTest_SimpleTest generated source
            await Assert.That(generatedCode).Contains("DerivedTest_SimpleTest_TestSource");
            
            // Find the LineNumber for the inherited test
            var lines = generatedCode.Split('\n');
            var lineNumberLineIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("DerivedTest_SimpleTest_TestSource") && 
                    i + 20 < lines.Length) // Look ahead for LineNumber
                {
                    for (int j = i; j < Math.Min(i + 30, lines.Length); j++)
                    {
                        if (lines[j].Contains("LineNumber = "))
                        {
                            lineNumberLineIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }
            
            await Assert.That(lineNumberLineIndex).IsGreaterThan(-1).Because("LineNumber should be found in generated code");
            
            var lineNumberLine = lines[lineNumberLineIndex].Trim();
            
            // Should be line 7 (where [Test] is in BaseTest) not line 14 (where [InheritsTests] is in DerivedTest)  
            await Assert.That(lineNumberLine).IsEqualTo("LineNumber = 7,")
                .Because("Inherited test should reference the line where [Test] attribute is located in the base class");
        });
}