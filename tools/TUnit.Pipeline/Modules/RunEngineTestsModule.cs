using ModularPipelines.Attributes;
using TUnit.Pipeline.Modules.Abstract;

namespace TUnit.Pipeline.Modules;

// The Reflection-mode engine tests, plus the tests that don't run per mode. They need no AOT app.
// aot.yml runs them in a job of their own: inside the dotnet.yml pipeline they waited for every
// other test module, so they ran last, alone, on the job's critical path.
[ModuleCategory(PipelineCategories.EngineTests)]
[NotInParallel("NetworkTests")]
public class RunEngineTestsModule : EngineTestsModuleBase
{
    protected override string TestMode => "Reflection";
}
