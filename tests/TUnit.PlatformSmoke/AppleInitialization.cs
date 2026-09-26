#if IOS || TVOS || MACCATALYST
using System.Runtime.CompilerServices;

namespace TUnit.PlatformSmoke;

internal static class AppleInitialization
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // The Apple SDK installs a UIKit context before Main. This console-style
        // test app has no UI event loop to execute captured async continuations.
        SynchronizationContext.SetSynchronizationContext(null);
    }
}
#endif
