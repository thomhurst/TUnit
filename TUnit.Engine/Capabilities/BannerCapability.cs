using System.Runtime.InteropServices;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.CommandLineProviders;

namespace TUnit.Engine.Capabilities;

#pragma warning disable TPEXP
internal class BannerCapability : IBannerMessageOwnerCapability
{
    const string Separator = " | ";

    private readonly IPlatformInformation _platformInformation;
    private readonly ICommandLineOptions _commandLineOptions;

    public BannerCapability(IPlatformInformation platformInformation, ICommandLineOptions commandLineOptions)
    {
        _platformInformation = platformInformation;
        _commandLineOptions = commandLineOptions;
    }
    
    public Task<string?> GetBannerMessageAsync()
    {
        if (_commandLineOptions.IsOptionSet(DisableLogoCommandProvider.DisableLogo))
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
            $"Microsoft Testing Platform v{_platformInformation.Version}"
        ];

        return string.Join(Separator, segments);
    }

    private string GetApplicationMemorySize()
    {
        return Environment.Is64BitProcess ? "64-bit" : "32-bit";
    }
}

    