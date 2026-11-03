namespace TUnit.Core.SourceGenerator.Tests;

internal class UninferredClassDataSourceTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
            "TUnit.TestProject", "Bugs", "_7355", "Tests.cs"),
        async generatedFiles =>
        {
            var failures = generatedFiles.Where(f => f.Contains("cannot be inferred at compile time")).ToArray();
            await Assert.That(failures.Length).IsEqualTo(5);
            var failure = failures.Single(f => f.Contains("InheritedUntypedClassSource"));
            await Assert.That(failure).Contains("RegisterGenericConstraintViolations");
            await Assert.That(failure).Contains("Use [GenerateGenericTest]");
            await Assert.That(failure).DoesNotContain("or a typed class data source");
            foreach (var className in new[] { "UninferredMethodWithInferredClass", "UninferredMethodWithExplicitClass" })
            {
                var methodFailure = failures.Single(f => f.Contains(className));
                await Assert.That(methodFailure).Contains("type arguments of generic test method");
                await Assert.That(methodFailure).Contains("on the method to specify its type arguments");
                await Assert.That(methodFailure).DoesNotContain("class type arguments cannot be inferred");
            }
            var bothUninferred = failures.Single(f => f.Contains("UninferredMethodAndClass"));
            await Assert.That(bothUninferred).Contains("class type arguments cannot be inferred");
            await Assert.That(generatedFiles.Count(f => f.Contains("RegisterEntries<"))).IsEqualTo(2);
        });
}
