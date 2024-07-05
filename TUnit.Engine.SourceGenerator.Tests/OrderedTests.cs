using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class OrderedTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "OrderedTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(6));
            
            Assert.That(generatedFiles[0], Does.Contain("TestName = \"Second\","));
            Assert.That(generatedFiles[0], Does.Contain("Order = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.NotInParallelAttribute>(attributes)?.Order ?? 1073741823,"));
            
            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Fourth\","));
            Assert.That(generatedFiles[1], Does.Contain("Order = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.NotInParallelAttribute>(attributes)?.Order ?? 1073741823,"));
            
            Assert.That(generatedFiles[2], Does.Contain("TestName = \"First\","));
            Assert.That(generatedFiles[2], Does.Contain("Order = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.NotInParallelAttribute>(attributes)?.Order ?? 1073741823,"));
            
            Assert.That(generatedFiles[3], Does.Contain("TestName = \"Fifth\","));
            Assert.That(generatedFiles[3], Does.Contain("Order = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.NotInParallelAttribute>(attributes)?.Order ?? 1073741823,"));
            
            Assert.That(generatedFiles[4], Does.Contain("TestName = \"Third\","));
            Assert.That(generatedFiles[4], Does.Contain("Order = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.NotInParallelAttribute>(attributes)?.Order ?? 1073741823,"));
            
            Assert.That(generatedFiles[5], Does.Contain("TestName = \"AssertOrder\","));
            Assert.That(generatedFiles[5], Does.Contain("Order = global::TUnit.Engine.Helpers.AttributeHelper.GetAttribute<global::TUnit.Core.NotInParallelAttribute>(attributes)?.Order ?? 1073741823,"));
        });
}