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
#if IOS || TVOS
        // The SDK's remote test adapter collects Documents/TestResults/*.trx.
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Directory.CreateDirectory(documents);
        Directory.SetCurrentDirectory(documents);
#endif
    }
}
#endif
