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
               
            """
        );
    }

    private string GetRuntimeDetails()
    {
        List<string> segments =
        [
            $"TUnit v{typeof(BannerCapability).Assembly.GetName().Version!.ToString()}",
            GetApplicationMemorySize(),
            RuntimeInformation.OSDescription,
            RuntimeInformation.RuntimeIdentifier,
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

    