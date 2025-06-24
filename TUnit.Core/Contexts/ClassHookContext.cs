using System;

namespace TUnit.Core.Contexts;

/// <summary>
/// Context used for class-level hooks (BeforeClass, AfterClass)
/// </summary>
public class ClassHookContext : ContextBase
{
    /// <summary>
    /// The test class type
    /// </summary>
    public Type TestClassType { get; }
    
    /// <summary>
    /// The test class instance (may be null for static classes or before instantiation)
    /// </summary>
    public object? TestClassInstance { get; internal set; }
    
    /// <summary>
    /// Number of tests in this class
    /// </summary>
    public int TestCount { get; internal set; }
    
    public ClassHookContext(Type testClassType)
    {
        TestClassType = testClassType ?? throw new ArgumentNullException(nameof(testClassType));
    }
}