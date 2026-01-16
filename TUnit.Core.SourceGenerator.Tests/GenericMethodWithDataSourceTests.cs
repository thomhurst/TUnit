using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class GenericMethodWithDataSourceTests : TestsBase
{
    [Test]
    public Task Generic_Method_With_MethodDataSource_Should_Generate_Tests() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "4431",
            "ComprehensiveGenericTests.cs"),
        async generatedFiles =>
        {
            // This test verifies that generic methods with [GenerateGenericTest] AND [MethodDataSource]
            // generate proper test metadata including both the type instantiations and the data source

            // Find all files related to GenericMethod_With_DataSource
            var matchingFiles = generatedFiles.Where(f =>
                f.Contains("GenericMethod_With_DataSource") &&
                f.Contains("TestSource")).ToList();

            // Debug: output the matching file count
            await Assert.That(matchingFiles.Count).IsGreaterThanOrEqualTo(1)
                .Because($"At least one test source should be generated for GenericMethod_With_DataSource. Found {matchingFiles.Count} matching files.");

            // Debug: output if any file contains Int32
            var hasInt32 = matchingFiles.Any(f => f.Contains("Int32"));
            var hasDouble = matchingFiles.Any(f => f.Contains("Double"));

            // Debug: Look at ConcreteInstantiations contents
            foreach (var file in matchingFiles)
            {
                if (file.Contains("ConcreteInstantiations"))
                {
                    var idx = file.IndexOf("ConcreteInstantiations");
                    var snippet = file.Substring(idx, Math.Min(500, file.Length - idx));
                    Console.WriteLine($"[DEBUG] ConcreteInstantiations snippet: {snippet}");
                }
            }

            // Find the file WITH concrete instantiations (uses typeof(int) and typeof(double) syntax)
            var genericMethodWithDataSourceFile = matchingFiles.FirstOrDefault(f =>
                f.Contains("typeof(int)") && f.Contains("typeof(double)"));

            await Assert.That(genericMethodWithDataSourceFile).IsNotNull()
                .Because("A test source should be generated for GenericMethod_With_DataSource with concrete type instantiations (int and double)");

            // If we found the file, verify it has both type instantiations
            if (genericMethodWithDataSourceFile != null)
            {
                // Verify it includes the MethodDataSource
                await Assert.That(genericMethodWithDataSourceFile).Contains("MethodDataSourceAttribute")
                    .Because("Should include the MethodDataSource attribute in DataSources");
                await Assert.That(genericMethodWithDataSourceFile).Contains("GetStrings")
                    .Because("Should reference the GetStrings method");
            }

            // Look for FullyGeneric_With_DataSources test generation (class+method generic with data source)
            var fullyGenericFiles = generatedFiles.Where(f =>
                f.Contains("FullyGeneric_With_DataSources") &&
                f.Contains("TestSource")).ToList();

            await Assert.That(fullyGenericFiles.Count).IsGreaterThanOrEqualTo(1)
                .Because("At least one test source should be generated for FullyGeneric_With_DataSources");

            // Find the file with concrete instantiations (uses typeof(int) or typeof(double) syntax)
            var fullyGenericFile = fullyGenericFiles.FirstOrDefault(f =>
                f.Contains("ConcreteInstantiations") &&
                (f.Contains("typeof(int)") || f.Contains("typeof(double)")));

            // Verify it has the data source
            if (fullyGenericFile != null)
            {
                await Assert.That(fullyGenericFile).Contains("MethodDataSourceAttribute")
                    .Because("Should include the MethodDataSource attribute in DataSources");
                await Assert.That(fullyGenericFile).Contains("GetBooleans")
                    .Because("Should reference the GetBooleans method");
            }
        });
}
