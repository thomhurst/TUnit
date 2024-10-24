using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ConstantArgumentsTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ConstantArgumentsTests.cs"),
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], 
                "global::System.String methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyString;");
            
            await AssertFileContains(generatedFiles[0], 
                "global::System.Int32 methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyInt;");
            
            await AssertFileContains(generatedFiles[0], 
                "global::System.Double methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyDouble;");
            
            await AssertFileContains(generatedFiles[0], 
                "global::System.Single methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyFloat;");
            
            await AssertFileContains(generatedFiles[0], 
                "global::System.Int64 methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyLong;");
            
            await AssertFileContains(generatedFiles[0], 
                "global::System.UInt32 methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyUInt;");
            
            await AssertFileContains(generatedFiles[0], 
                "global::System.UInt64 methodArg = global::TUnit.TestProject.ConstantArgumentsTests.DummyULong;");
        });
}