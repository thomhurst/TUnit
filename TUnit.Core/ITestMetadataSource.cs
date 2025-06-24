using System.Collections.Generic;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Interface for test metadata sources
/// </summary>
public interface ITestMetadataSource
{
    /// <summary>
    /// Gets test metadata from this source
    /// </summary>
    Task<IEnumerable<TestMetadata>> GetTestMetadata();
}