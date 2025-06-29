using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Enums;

namespace TUnit.Engine.Capabilities;

#pragma warning disable TPEXP
internal class BannerCapability(IPlatformInformation platformInformation, ICommandLineOptions commandLineOptions)
    : IBannerMessageOwnerCapability
{
    const string Separator = " | ";

    public Task<string?> GetBannerMessageAsync()
    {
        if (commandLineOptions.IsOptionSet(DisableLogoCommandProvider.DisableLogo))
        {
            return Task.FromResult<string?>(GetRuntimeDetails());
        }

        var stringBuilder = new StringBuilder(
            $"""

             ████████╗██╗   ██╗███╗   ██╗██╗████████╗
             ╚══██╔══╝██║   ██║████╗  ██║██║╚══██╔══╝
                ██║   ██║   ██║██╔██╗ ██║██║   ██║
                ██║   ██║   ██║██║╚██╗██║██║   ██║
                ██║   ╚██████╔╝██║ ╚████║██║   ██║
                ╚═╝    ╚═════╝ ╚═╝  ╚═══╝╚═╝   ╚═╝

                {GetRuntimeDetails()}

                Engine Mode: {GetMode()}

             """
        );

#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            stringBuilder.Append(
                """

                    AOT compilation detected. Dynamic code generation is not supported.

                """
            );
        }
#endif

        return Task.FromResult<string?>(
            stringBuilder.ToString()
        );
    }

    private EngineMode GetMode()
    {
        if (commandLineOptions.IsOptionSet(ReflectionScannerCommandProvider.ReflectionScanner)
            || Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Any(x => x is { Key: "TUnitReflectionScanner", Value: "true" }) is true)
        {
            return EngineMode.Reflection;
        }

        return EngineMode.SourceGenerated;
    }

    private string GetRuntimeDetails()
    {
        List<string> segments =
        [
            $"TUnit v{typeof(BannerCapability).Assembly.GetName().Version!}",
            GetApplicationMemorySize(),
            RuntimeInformation.OSDescription,
#if NET
            RuntimeInformation.RuntimeIdentifier,
#else
            ".NET Framework",
#endif
            RuntimeInformation.FrameworkDescription,
            $"Microsoft Testing Platform v{platformInformation.Version}"
        ];

        return string.Join(Separator, segments);
    }

    private string GetApplicationMemorySize()
    {
        return Environment.Is64BitProcess ? "64-bit" : "32-bit";
    }
}

