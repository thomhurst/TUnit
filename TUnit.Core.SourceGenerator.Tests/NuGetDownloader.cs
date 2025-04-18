using System.IO.Compression;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace TUnit.Core.SourceGenerator.Tests;

public static class NuGetDownloader
{
    private static SourceCacheContext CacheContext = new();
    private static ILogger Logger = NullLogger.Instance;
    private static string OutputPath = Path.Combine(Path.GetTempPath(), "TUnit.Core.SourceGenerator.Tests", "NuGetPackages");
    
    public static async Task<IEnumerable<MetadataReference>> DownloadPackageAsync(string packageId, string version)
    {
        var extractedPath = Path.Combine(OutputPath, $"{packageId}.{version}");

        if (!Directory.Exists(extractedPath))
        {

            var settings = Settings.LoadDefaultSettings(null);
            var sourceRepositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());
            var repository = sourceRepositoryProvider.CreateRepository(new PackageSource("https://api.nuget.org/v3/index.json"));

            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            Directory.CreateDirectory(OutputPath);

            var packagePath = Path.Combine(OutputPath, $"{packageId}.{version}.nupkg");

            using (var packageStream = File.Create(packagePath))
            {
                await resource.CopyNupkgToStreamAsync(packageId, NuGetVersion.Parse(version), packageStream, CacheContext, Logger, CancellationToken.None);
            }

            Directory.CreateDirectory(extractedPath);

            using (var zip = new ZipArchive(File.OpenRead(packagePath), ZipArchiveMode.Read))
            {
                zip.ExtractToDirectory(extractedPath);
            }
        }
        
        var files = Directory.EnumerateFiles(extractedPath, "*.dll", SearchOption.AllDirectories);
            
        return files
            .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(x => MetadataReference.CreateFromFile(x));
    }
}