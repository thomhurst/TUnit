using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class TestArtifactTests
{
    [Test]
    public void Artifact_Test()
    {
        TestContext.Current!.Output.AttachArtifact(new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Blah!"
        });
    }

    [Test]
    public async Task TestLevel_SingleArtifact_CanBeAccessedAfterAdding()
    {
        // Arrange
        var testContext = TestContext.Current!;
        var artifactFile = new FileInfo("Data/Zip.zip");
        var expectedDisplayName = "Test Artifact";
        var expectedDescription = "A test artifact for validation";

        // Act
        testContext.Output.AttachArtifact(new Artifact
        {
            File = artifactFile,
            DisplayName = expectedDisplayName,
            Description = expectedDescription
        });

        // Assert
        var artifacts = testContext.Output.Artifacts;
        await Assert.That(artifacts.Count).IsEqualTo(1);

        var artifact = artifacts.First();
        await Assert.That(artifact.File.FullName).IsEqualTo(artifactFile.FullName);
        await Assert.That(artifact.DisplayName).IsEqualTo(expectedDisplayName);
        await Assert.That(artifact.Description).IsEqualTo(expectedDescription);
    }

    [Test]
    public async Task TestLevel_MultipleArtifacts_AllCanBeAccessed()
    {
        // Arrange
        var testContext = TestContext.Current!;
        var artifact1 = new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Artifact 1"
        };
        var artifact2 = new Artifact
        {
            File = new FileInfo("Data/Blah.txt"),
            DisplayName = "Artifact 2",
            Description = "Second artifact"
        };
        var artifact3 = new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Artifact 3",
            Description = "Third artifact"
        };

        // Act
        testContext.Output.AttachArtifact(artifact1);
        testContext.Output.AttachArtifact(artifact2);
        testContext.Output.AttachArtifact(artifact3);

        // Assert
        var artifacts = testContext.Output.Artifacts;
        await Assert.That(artifacts.Count).IsEqualTo(3);

        // Verify all artifacts are present
        await Assert.That(artifacts).Contains(artifact1);
        await Assert.That(artifacts).Contains(artifact2);
        await Assert.That(artifacts).Contains(artifact3);

        // Verify display names
        var displayNames = artifacts.Select(a => a.DisplayName).ToList();
        await Assert.That(displayNames).Contains("Artifact 1");
        await Assert.That(displayNames).Contains("Artifact 2");
        await Assert.That(displayNames).Contains("Artifact 3");
    }

    [Test]
    public async Task TestLevel_Artifact_WithoutDescription_AccessibleAfterAdding()
    {
        // Arrange
        var testContext = TestContext.Current!;
        var artifact = new Artifact
        {
            File = new FileInfo("Data/Blah.txt"),
            DisplayName = "Text File Artifact"
        };

        // Act
        testContext.Output.AttachArtifact(artifact);

        // Assert
        var artifacts = testContext.Output.Artifacts;
        await Assert.That(artifacts.Count).IsEqualTo(1);

        var retrievedArtifact = artifacts.First();
        await Assert.That(retrievedArtifact.DisplayName).IsEqualTo("Text File Artifact");
        await Assert.That(retrievedArtifact.Description).IsNull();
    }

    [Test]
    public async Task TestLevel_Artifacts_PreserveFileInfo()
    {
        // Arrange
        var testContext = TestContext.Current!;
        var fileInfo = new FileInfo("Data/Zip.zip");
        var artifact = new Artifact
        {
            File = fileInfo,
            DisplayName = "Zip Archive"
        };

        // Act
        testContext.Output.AttachArtifact(artifact);

        // Assert
        var artifacts = testContext.Output.Artifacts;
        var retrievedArtifact = artifacts.First();

        await Assert.That(retrievedArtifact.File).IsNotNull();
        await Assert.That(retrievedArtifact.File.Name).IsEqualTo(fileInfo.Name);
        await Assert.That(retrievedArtifact.File.FullName).IsEqualTo(fileInfo.FullName);
    }

    [Test]
    public async Task TestLevel_Artifacts_EmptyInitially()
    {
        // Arrange
        var testContext = TestContext.Current!;

        // Assert - no artifacts added yet
        var artifacts = testContext.Output.Artifacts;
        await Assert.That(artifacts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TestLevel_Artifacts_AreReadOnly()
    {
        // Arrange
        var testContext = TestContext.Current!;
        testContext.Output.AttachArtifact(new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Test"
        });

        // Assert - verify collection is read-only
        var artifacts = testContext.Output.Artifacts;
        await Assert.That(artifacts).IsTypeOf<IReadOnlyCollection<Artifact>>();
    }

    [Test]
    public async Task SessionLevel_SingleArtifact_CanBeAccessedAfterAdding()
    {
        // Arrange
        var sessionContext = TestSessionContext.Current!;
        var artifactFile = new FileInfo("Data/Zip.zip");
        var expectedDisplayName = "Session Artifact";
        var expectedDescription = "A session-level test artifact";

        // Act
        sessionContext.AddArtifact(new Artifact
        {
            File = artifactFile,
            DisplayName = expectedDisplayName,
            Description = expectedDescription
        });

        // Assert
        var artifacts = sessionContext.Artifacts;
        await Assert.That(artifacts.Count).IsGreaterThanOrEqualTo(1);

        var addedArtifact = artifacts.FirstOrDefault(a => a.DisplayName == expectedDisplayName);
        await Assert.That(addedArtifact).IsNotNull();
        await Assert.That(addedArtifact!.File.FullName).IsEqualTo(artifactFile.FullName);
        await Assert.That(addedArtifact.Description).IsEqualTo(expectedDescription);
    }

    [Test]
    public async Task SessionLevel_MultipleArtifacts_AllCanBeAccessed()
    {
        // Arrange
        var sessionContext = TestSessionContext.Current!;
        var artifact1 = new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Session Artifact 1"
        };
        var artifact2 = new Artifact
        {
            File = new FileInfo("Data/Blah.txt"),
            DisplayName = "Session Artifact 2",
            Description = "Second session artifact"
        };

        var initialCount = sessionContext.Artifacts.Count;

        // Act
        sessionContext.AddArtifact(artifact1);
        sessionContext.AddArtifact(artifact2);

        // Assert
        var artifacts = sessionContext.Artifacts;
        await Assert.That(artifacts.Count).IsEqualTo(initialCount + 2);

        // Verify artifacts are present
        await Assert.That(artifacts).Contains(artifact1);
        await Assert.That(artifacts).Contains(artifact2);
    }

    [Test]
    public async Task SessionLevel_Artifact_WithoutDescription_AccessibleAfterAdding()
    {
        // Arrange
        var sessionContext = TestSessionContext.Current!;
        var artifact = new Artifact
        {
            File = new FileInfo("Data/Blah.txt"),
            DisplayName = "Session Text File"
        };

        var initialCount = sessionContext.Artifacts.Count;

        // Act
        sessionContext.AddArtifact(artifact);

        // Assert
        var artifacts = sessionContext.Artifacts;
        await Assert.That(artifacts.Count).IsEqualTo(initialCount + 1);

        var addedArtifact = artifacts.FirstOrDefault(a => a.DisplayName == "Session Text File");
        await Assert.That(addedArtifact).IsNotNull();
        await Assert.That(addedArtifact!.Description).IsNull();
    }

    [Test]
    public async Task SessionLevel_Artifacts_PreserveFileInfo()
    {
        // Arrange
        var sessionContext = TestSessionContext.Current!;
        var fileInfo = new FileInfo("Data/Zip.zip");
        var artifact = new Artifact
        {
            File = fileInfo,
            DisplayName = "Session Zip Archive"
        };

        // Act
        sessionContext.AddArtifact(artifact);

        // Assert
        var artifacts = sessionContext.Artifacts;
        var addedArtifact = artifacts.FirstOrDefault(a => a.DisplayName == "Session Zip Archive");

        await Assert.That(addedArtifact).IsNotNull();
        await Assert.That(addedArtifact!.File).IsNotNull();
        await Assert.That(addedArtifact.File.Name).IsEqualTo(fileInfo.Name);
        await Assert.That(addedArtifact.File.FullName).IsEqualTo(fileInfo.FullName);
    }

    [Test]
    public async Task TestAndSessionLevel_Artifacts_AreIndependent()
    {
        // Arrange
        var testContext = TestContext.Current!;
        var sessionContext = TestSessionContext.Current!;

        var testArtifact = new Artifact
        {
            File = new FileInfo("Data/Zip.zip"),
            DisplayName = "Test Level Artifact Independent"
        };

        var sessionArtifact = new Artifact
        {
            File = new FileInfo("Data/Blah.txt"),
            DisplayName = "Session Level Artifact Independent"
        };

        // Act
        testContext.Output.AttachArtifact(testArtifact);
        sessionContext.AddArtifact(sessionArtifact);

        // Assert
        var testArtifacts = testContext.Output.Artifacts;
        var sessionArtifacts = sessionContext.Artifacts;

        // Test artifacts should only contain test-level artifact from this test
        await Assert.That(testArtifacts.Count).IsEqualTo(1);
        await Assert.That(testArtifacts).Contains(testArtifact);
        await Assert.That(testArtifacts.Any(a => a.DisplayName == "Session Level Artifact Independent")).IsFalse();

        // Session artifacts should contain session-level artifact (session persists across tests, so just verify it exists)
        await Assert.That(sessionArtifacts).Contains(sessionArtifact);
        await Assert.That(sessionArtifacts.Any(a => a.DisplayName == "Test Level Artifact Independent")).IsFalse();
    }

    [Test]
    public async Task TestLevel_ArtifactProperties_AreImmutable()
    {
        // Arrange
        var testContext = TestContext.Current!;
        var fileInfo = new FileInfo("Data/Zip.zip");
        var displayName = "Immutable Artifact";
        var description = "This artifact should be immutable";

        var artifact = new Artifact
        {
            File = fileInfo,
            DisplayName = displayName,
            Description = description
        };

        // Act
        testContext.Output.AttachArtifact(artifact);

        // Assert - retrieve and verify properties haven't changed
        var artifacts = testContext.Output.Artifacts;
        var retrievedArtifact = artifacts.First();

        await Assert.That(retrievedArtifact.File.FullName).IsEqualTo(fileInfo.FullName);
        await Assert.That(retrievedArtifact.DisplayName).IsEqualTo(displayName);
        await Assert.That(retrievedArtifact.Description).IsEqualTo(description);
    }
}
