using System;
using System.Collections.Generic;

namespace TUnit.Core;

/// <summary>
/// Context passed to hook methods during test execution
/// </summary>
public sealed class HookContext
{
    /// <summary>
    /// The test context for the current test
    /// </summary>
    public TestContext TestContext { get; }
    
    /// <summary>
    /// The type containing the test
    /// </summary>
    public Type TestClassType { get; }
    
    /// <summary>
    /// The test class instance (null for static hooks or before instantiation)
    /// </summary>
    public object? TestInstance { get; }
    
    /// <summary>
    /// Additional data that can be shared between hooks
    /// </summary>
    public Dictionary<string, object?> Items { get; } = new();
    
    public HookContext(TestContext testContext, Type testClassType, object? testInstance)
    {
        TestContext = testContext ?? throw new ArgumentNullException(nameof(testContext));
        TestClassType = testClassType ?? throw new ArgumentNullException(nameof(testClassType));
        TestInstance = testInstance;
    }
}