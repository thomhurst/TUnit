using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public class BaseHookWriter
{
    protected static string GetArgs(HooksDataModel model)
    {
        List<string> args = [];

        var expectedType = model.HookLevel switch
        {
            "TUnit.Core.HookType.Test" => WellKnownFullyQualifiedClassNames.TestContext,
            "TUnit.Core.HookType.Class" => WellKnownFullyQualifiedClassNames.ClassHookContext,
            "TUnit.Core.HookType.Assembly" => WellKnownFullyQualifiedClassNames.AssemblyHookContext,
            "TUnit.Core.HookType.TestSession" => WellKnownFullyQualifiedClassNames.TestSessionContext,
            "TUnit.Core.HookType.TestDiscovery" when model.HookLocationType == HookLocationType.Before => WellKnownFullyQualifiedClassNames.BeforeTestDiscoveryContext,
            "TUnit.Core.HookType.TestDiscovery" when model.HookLocationType == HookLocationType.After => WellKnownFullyQualifiedClassNames.TestDiscoveryContext,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        foreach (var type in model.ParameterTypes)
        {
            if (type == expectedType.WithGlobalPrefix)
            {
                args.Add("context");
            }

            if (type == WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix)
            {
                args.Add("cancellationToken");
            }
        }

        return string.Join(", ", args);
    }
}