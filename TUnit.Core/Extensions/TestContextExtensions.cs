using System;
using System.Linq;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

/// <summary>
/// Simplified extension methods for TestContext
/// </summary>
public static class TestContextExtensions
{
    /// <summary>
    /// Gets a service from the test context
    /// </summary>
    public static T? GetService<T>(this TestContext context) where T : class
    {
        return context.GetService<T>();
    }
    
    /// <summary>
    /// Gets the class type name
    /// </summary>
    public static string GetClassTypeName(this TestContext context)
    {
        return context.TestDetails?.ClassType?.Name ?? "Unknown";
    }
    
    /// <summary>
    /// Gets the test display name
    /// </summary>
    public static string GetDisplayName(this TestContext context)
    {
        return context.DisplayName;
    }
}