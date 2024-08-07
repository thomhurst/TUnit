using System.Runtime.InteropServices;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Services;

namespace TUnit.Engine.Capabilities;

#pragma warning disable TPEXP
internal class BannerCapability : IBannerMessageOwnerCapability
{
    const string Separator = " | ";

    private readonly IPlatformInformation _platformInformation;

    public BannerCapability(IPlatformInformation platformInformation)
    {
        _platformInformation = platformInformation;
    }
    
    public Task<string?> GetBannerMessageAsync()
    {
        return Task.FromResult<string?>(
            $"""
                                                                                                       
            ████████╗██╗   ██╗███╗   ██╗██╗████████╗
            ╚══██╔══╝██║   ██║████╗  ██║██║╚══██╔══╝
               ██║   ██║   ██║██╔██╗ ██║██║   ██║   
               ██║   ██║   ██║██║╚██╗██║██║   ██║   
               ██║   ╚██████╔╝██║ ╚████║██║   ██║   
               ╚═╝    ╚═════╝ ╚═╝  ╚═══╝╚═╝   ╚═╝   
               
               {GetRuntimeDetails()}
               
               {GetBuildDetails()}
               
            """
        );
    }

    private string GetRuntimeDetails()
    {
        var segments = new List<string>();

        segments.Add($"v{typeof(BannerCapability).Assembly.GetName().Version!.ToString()}");
        segments.Add(GetApplicationMemorySize());
        segments.Add(RuntimeInformation.OSDescription);
        segments.Add(RuntimeInformation.RuntimeIdentifier);
        segments.Add(RuntimeInformation.FrameworkDescription);
        
        return string.Join(Separator, segments);
    }
    
    private string GetBuildDetails()
    {
        var segments = new List<string>();
        
        if (_platformInformation.BuildDate != null)
        {
            segments.Add(_platformInformation.BuildDate.Value.ToString());
        }
        
        if (_platformInformation.CommitHash != null)
        {
            segments.Add($"Commit #{_platformInformation.CommitHash}");
        }
        
        return string.Join(Separator, segments);
    }

    private string GetApplicationMemorySize()
    {
        return Environment.Is64BitProcess ? "64-bit" : "32-bit";
    }
}

    