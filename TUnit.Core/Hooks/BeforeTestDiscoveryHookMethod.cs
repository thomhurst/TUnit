﻿namespace TUnit.Core.Hooks;

public record BeforeTestDiscoveryHookMethod : StaticHookMethod<BeforeTestDiscoveryContext>
{
    public override ValueTask ExecuteAsync(BeforeTestDiscoveryContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteBeforeTestDiscoveryHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
