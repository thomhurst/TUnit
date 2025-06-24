using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Source for test metadata
/// </summary>
public interface ITestMetadataSource
{
    /// <summary>
    /// Gets test metadata from this source
    /// </summary>
    Task<IEnumerable<TestMetadata>> GetTestMetadata();
}