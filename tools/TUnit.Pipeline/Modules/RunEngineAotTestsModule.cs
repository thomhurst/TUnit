using ModularPipelines.Attributes;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The AOT-mode engine tests, which run the app PublishTestProjectAOTModule publishes. The filter
// leaves out the mode-agnostic tests RunEngineTestsModule already ran. They run in aot.yml, on
// runners of their own, so they need not wait for the other test modules.
[ModuleCategory(PipelineCategories.Aot)]
[DependsOn<PublishTestProjectAOTModule>]
public class RunEngineAotTestsModule : EngineTestsModuleBase
{
    protected override string TestMode => "AOT";

    protected override IEnumerable<string> AdditionalArguments =>
        ["--treenode-filter", "/*/*/*/*[EngineTestMode=AOT]"];
}
