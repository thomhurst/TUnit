using ModularPipelines.Attributes;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The Reflection-mode engine tests, plus the tests that don't run per mode. They need no AOT app,
// but they still wait for its publish: the publish is the critical path (RunEngineAotTestsModule
// waits for it), and these tests start many TestProject processes. Running them alongside it
// slowed the publish 2-5x on every runner OS.
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
[DependsOn<PublishTestProjectAOTModule>]
public class RunEngineTestsModule : EngineTestsModuleBase
{
    protected override string TestMode => "Reflection";
}
