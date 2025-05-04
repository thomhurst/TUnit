using System.Runtime.InteropServices;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.CommandLineProviders;

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
        
        return Task.FromResult<string?>(
            $"""
            
            ████████╗██╗   ██╗███╗   ██╗██╗████████╗
            ╚══██╔══╝██║   ██║████╗  ██║██║╚══██╔══╝
               ██║   ██║   ██║██╔██╗ ██║██║   ██║   
               ██║   ██║   ██║██║╚██╗██║██║   ██║   
               ██║   ╚██████╔╝██║ ╚████║██║   ██║   
               ╚═╝    ╚═════╝ ╚═╝  ╚═══╝╚═╝   ╚═╝   
               
               {GetRuntimeDetails()}
               
               {GetMode()}
            """
        );
    }

    private string GetMode()
    {
        if (commandLineOptions.IsOptionSet(ReflectionScannerCommandProvider.ReflectionScanner))
        {
            return """
                   Finding tests via Reflection Scanning...

                   """;
        }

        return string.Empty;
    }

    private string GetRuntimeDetails()
    {
        List<string> segments =
        [
            $"TUnit v{typeof(BannerCapability).Assembly.GetName().Version!.ToString()}",
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

    