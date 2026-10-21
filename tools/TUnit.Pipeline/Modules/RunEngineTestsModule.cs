using ModularPipelines.Attributes;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The Reflection-mode engine tests, plus the tests that don't run per mode. They need no AOT app:
// the AOT publish and the AOT-mode engine tests run in aot.yml instead.
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
}
