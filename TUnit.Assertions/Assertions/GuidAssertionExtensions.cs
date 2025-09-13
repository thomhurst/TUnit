using System;
using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Note: Guid.TryParse has different signatures in different frameworks
// We'll skip this for now as it requires special handling

[CreateAssertion<Guid>( typeof(GuidAssertionExtensions), nameof(IsEmpty))]
[CreateAssertion<Guid>( typeof(GuidAssertionExtensions), nameof(IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]

[CreateAssertion<Guid?>( typeof(GuidAssertionExtensions), nameof(IsNullOrEmpty))]
[CreateAssertion<Guid?>( typeof(GuidAssertionExtensions), nameof(IsNullOrEmpty), CustomName = "IsNotNullOrEmpty", NegateLogic = true)]
public static partial class GuidAssertionExtensions
{
    // Helper methods for Guid assertions
    internal static bool IsEmpty(Guid guid) => guid == Guid.Empty;
    
    internal static bool IsNullOrEmpty(Guid? guid) => !guid.HasValue || guid.Value == Guid.Empty;
}
