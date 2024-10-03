using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Extensions;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class NullableByteArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NullableByteArgumentTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(4));

            Assert.That(generatedFiles[0], Does.Contain(
                """
                global::System.Byte? methodArg = (global::System.Byte)1;
                """));
            
            Assert.That(generatedFiles[1], Does.Contain(
                """
                global::System.Byte? methodArg = null;
                """));
            
            Assert.That(generatedFiles[2].IgnoreWhitespaceFormatting(), Does.Contain(
                """
                global::System.Byte methodArg = (global::System.Byte)1;
                global::System.Byte? methodArg1 = (global::System.Byte)1;
                """.IgnoreWhitespaceFormatting()));
            
            Assert.That(generatedFiles[3].IgnoreWhitespaceFormatting(), Does.Contain(
                """
                global::System.Byte methodArg = (global::System.Byte)1;
                global::System.Byte? methodArg1 = null;
                """.IgnoreWhitespaceFormatting()));
        });
}