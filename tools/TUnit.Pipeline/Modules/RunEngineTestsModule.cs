using ModularPipelines.Attributes;
using ModularPipelines.Modules;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The Reflection-mode engine tests, plus the tests that don't run per mode. They need no AOT app,
// so they start as soon as the other test modules have freed the CPUs.
[NotInParallel("NetworkTests")]
[DependsOn<RunAnalyzersTestsModule>]
[DependsOn<RunUnitTestsModule>]
[DependsOn<RunTemplateTestsModule>]
[DependsOn<RunAspNetTestsModule>(Optional = true)]
[DependsOn<RunAssertionsTestsModule>]
[DependsOn<RunAssertionsShouldTestsModule>]
[DependsOn<RunAssertionsShouldSourceGeneratorTestsModule>]
[DependsOn<RunAssertionsAnalyzersTestsModule>]
[DependsOn<RunPublicAPITestsModule>]
[DependsOn<RunSourceGeneratorTestsModule>]
[DependsOn<RunAssertionsCodeFixersTestsModule>]
public class RunEngineTestsModule : EngineTestsModuleBase
{
    protected override string TestMode => "Reflection";

    // On the 3-core macOS runner, these tests overlapping the AOT test-app publish slowed both,
    // and the publish took about 25 minutes instead of 7. There they wait for it, as they did before
    // #7131. On Linux and Windows the overlap is cheap, so they still start early.
    protected override void DeclareDependencies(IDependencyDeclaration deps)
    {
        if (OperatingSystem.IsMacOS())
        {
            deps.DependsOnOptional<PublishTestProjectAOTModule>();
        }
    }
}
