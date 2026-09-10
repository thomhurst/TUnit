using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Scans assemblies for test metadata (reflection-based)
/// </summary>
internal interface ITestMetadataScanner
{
    Task<IEnumerable<TestMetadata>> ScanAssembly(Assembly assembly);
}
