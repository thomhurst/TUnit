namespace TUnit.Engine.Enums;

/// <summary>
/// Categorizes test failures to help users quickly understand what went wrong.
/// </summary>
internal enum FailureCategory
{
    /// <summary>
    /// TUnit assertion failure (AssertionException).
    /// </summary>
    Assertion,

    /// <summary>
    /// Timeout or cancellation failure (OperationCanceledException, TaskCanceledException, TimeoutException).
    /// </summary>
    Timeout,

    /// <summary>
    /// NullReferenceException in user code.
    /// </summary>
    NullReference,

    /// <summary>
    /// Failure in a Before/BeforeEvery hook.
    /// </summary>
    Setup,

    /// <summary>
    /// Failure in an After/AfterEvery hook.
    /// </summary>
    Teardown,

    /// <summary>
    /// File, network, or other I/O exception.
    /// </summary>
    Infrastructure,

    /// <summary>
    /// Unrecognized failure type.
    /// </summary>
    Unknown
}
