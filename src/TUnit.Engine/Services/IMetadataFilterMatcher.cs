using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Service for evaluating if test metadata could match an execution filter.
/// Provides conservative matching without requiring full test building.
/// </summary>
internal interface IMetadataFilterMatcher
{
    /// <summary>
    /// Determines if test metadata could potentially match the filter.
    /// Returns true unless we can definitively rule out the test.
    /// </summary>
    /// <param name="metadata">The test metadata to evaluate</param>
    /// <param name="filter">The execution filter to match against (null means match all)</param>
    /// <returns>True if the metadata could match the filter, false if it definitely doesn't</returns>
    bool CouldMatchFilter(TestMetadata metadata, ITestExecutionFilter? filter);
}
