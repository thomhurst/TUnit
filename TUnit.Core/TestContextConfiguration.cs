using System;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Extension methods for typed access to TestContext configuration
/// </summary>
public static class TestContextConfiguration
{
    private const string DisplayNameFormatterKey = "DisplayNameFormatter";
    private const string ShouldRetryFuncKey = "ShouldRetryFunc";
    private const string ParallelConstraintKey = "ParallelConstraint";
    private const string ParallelLimiterKey = "ParallelLimiter";
    private const string SkipReasonKey = "SkipReason";
    
    /// <summary>
    /// Gets the display name formatter type if set
    /// </summary>
    public static Type? GetDisplayNameFormatter(this TestContext context)
    {
        return context.DisplayNameFormatter;
    }
    
    /// <summary>
    /// Gets the retry function if set
    /// </summary>
    public static Func<TestContext, Exception, int, Task<bool>>? GetShouldRetryFunc(this TestContext context)
    {
        return context.ShouldRetryFunc;
    }
    
    /// <summary>
    /// Gets the parallel constraint if set
    /// </summary>
    public static IParallelConstraint? GetParallelConstraint(this TestContext context)
    {
        return context.ParallelConstraint;
    }
    
    /// <summary>
    /// Gets the parallel limiter if set
    /// </summary>
    public static IParallelLimit? GetParallelLimiter(this TestContext context)
    {
        return context.ParallelLimiter;
    }
    
    /// <summary>
    /// Gets the skip reason if set
    /// </summary>
    public static string? GetSkipReason(this TestContext context)
    {
        return context.SkipReason;
    }
    
    /// <summary>
    /// Sets the skip reason
    /// </summary>
    public static void SetSkipReason(this TestContext context, string reason)
    {
        context.SkipReason = reason;
    }
}