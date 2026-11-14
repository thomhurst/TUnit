using DiffEngine;

namespace TUnit.Templates.Tests;

public class GlobalSetup
{
    [Before(TestDiscovery)]
    public static void BeforeTestDiscovery()
    {
        DiffRunner.Disabled = true;

        // TemplateVerifier 11 no longer bundles a test framework's Verify adapter
        VerificationEngine.DirectoryVerifier = (path, include, pattern, options, settings, info, fileScrubber, sourceFile) =>
            Verifier.VerifyDirectory(path, include, pattern, options, settings, info, fileScrubber, sourceFile);
    }
}
