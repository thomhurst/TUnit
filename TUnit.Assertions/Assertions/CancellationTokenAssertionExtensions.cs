using System.Threading;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<CancellationToken>( nameof(CancellationToken.IsCancellationRequested))]
[CreateAssertion<CancellationToken>( nameof(CancellationToken.IsCancellationRequested), CustomName = "IsNotCancellationRequested", NegateLogic = true)]

[CreateAssertion<CancellationToken>( nameof(CancellationToken.CanBeCanceled))]
[CreateAssertion<CancellationToken>( nameof(CancellationToken.CanBeCanceled), CustomName = "CannotBeCanceled", NegateLogic = true)]

// Helper for checking if it's the default/none token
[CreateAssertion<CancellationToken>( typeof(CancellationTokenAssertionExtensions), nameof(IsNone))]
[CreateAssertion<CancellationToken>( typeof(CancellationTokenAssertionExtensions), nameof(IsNone), CustomName = "IsNotNone", NegateLogic = true)]
public static partial class CancellationTokenAssertionExtensions
{
    internal static bool IsNone(CancellationToken token) => token == CancellationToken.None;
}