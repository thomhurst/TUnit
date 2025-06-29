using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Assembly-based test metadata source for dynamic discovery
/// </summary>
public sealed class AssemblyTestMetadataSource : ITestMetadataSource
{
    private readonly Assembly _assembly;
    private readonly ITestMetadataScanner _scanner;
    
    public AssemblyTestMetadataSource(Assembly assembly, ITestMetadataScanner scanner)
    {
        _assembly = assembly;
        _scanner = scanner;
    }
    
    public async Task<IEnumerable<TestMetadata>> GetTestMetadata()
    {
        return await _scanner.ScanAssembly(_assembly);
    }
}