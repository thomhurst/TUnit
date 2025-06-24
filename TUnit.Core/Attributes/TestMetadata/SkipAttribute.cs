using System;
using System.Threading.Tasks;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Simplified attribute to skip tests
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class SkipAttribute : Attribute, ITestRegisteredEventReceiver
{
    /// <summary>
    /// Gets the reason why the test is skipped
    /// </summary>
    public string Reason { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SkipAttribute"/> class
    /// </summary>
    /// <param name="reason">The reason for skipping the test</param>
    public SkipAttribute(string reason)
    {
        Reason = reason;
    }
    
    /// <inheritdoc />
    public int Order => int.MinValue;
    
    /// <inheritdoc />
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (await ShouldSkip(context))
        {
            // Store skip reason directly on TestContext
            context.TestContext.SkipReason = Reason;
        }
    }
    
    /// <summary>
    /// Determines whether the test should be skipped
    /// </summary>
    public virtual Task<bool> ShouldSkip(TestRegisteredContext context) => Task.FromResult(true);
}