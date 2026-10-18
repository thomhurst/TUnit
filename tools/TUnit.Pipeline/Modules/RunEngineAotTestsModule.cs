using ModularPipelines.Attributes;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The AOT-mode engine tests, which run the app PublishTestProjectAOTModule publishes. The filter
// leaves out the mode-agnostic tests RunEngineTestsModule already ran.
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
public class RunEngineAotTestsModule : EngineTestsModuleBase
{
    protected override string TestMode => "AOT";

    protected override IEnumerable<string> AdditionalArguments =>
        ["--treenode-filter", "/*/*/*/*[EngineTestMode=AOT]"];
}
