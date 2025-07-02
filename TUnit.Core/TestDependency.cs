using System;
using System.Linq;

namespace TUnit.Core;

/// <summary>
/// Represents a dependency on another test with support for generic types and methods
/// </summary>
public sealed class TestDependency : IEquatable<TestDependency>
{
    /// <summary>
    /// The type containing the test method
    /// </summary>
    public Type? ClassType { get; init; }
    
    /// <summary>
    /// Number of generic type parameters on the class (0 for non-generic classes)
    /// </summary>
    public int ClassGenericArity { get; init; }
    
    /// <summary>
    /// The name of the test method
    /// </summary>
    public string? MethodName { get; init; }
    
    /// <summary>
    /// Parameter types of the method (null if not specified)
    /// </summary>
    public Type[]? MethodParameters { get; init; }
    
    /// <summary>
    /// Number of generic type parameters on the method (0 for non-generic methods)
    /// </summary>
    public int MethodGenericArity { get; init; }
    
    /// <summary>
    /// Creates a dependency on a specific test method by name within the same class
    /// </summary>
    public static TestDependency FromMethodName(string methodName)
    {
        return new TestDependency { MethodName = methodName };
    }
    
    /// <summary>
    /// Creates a dependency on all tests in a specific class
    /// </summary>
    public static TestDependency FromClass(Type classType)
    {
        return new TestDependency 
        { 
            ClassType = classType,
            ClassGenericArity = classType.IsGenericTypeDefinition ? classType.GetGenericArguments().Length : 0
        };
    }
    
    /// <summary>
    /// Creates a dependency on a specific test method in a specific class
    /// </summary>
    public static TestDependency FromClassAndMethod(Type classType, string methodName)
    {
        return new TestDependency 
        { 
            ClassType = classType,
            ClassGenericArity = classType.IsGenericTypeDefinition ? classType.GetGenericArguments().Length : 0,
            MethodName = methodName 
        };
    }
    
    /// <summary>
    /// Checks if this dependency matches the given test metadata
    /// </summary>
    public bool Matches(TestMetadata test, TestMetadata? dependentTest = null)
    {
        // If ClassType is specified, it must match
        if (ClassType != null)
        {
            var testClassType = test.TestClassType;
            
            // Handle generic type matching
            if (ClassType.IsGenericTypeDefinition && testClassType.IsGenericType)
            {
                // Match generic type definition
                if (testClassType.GetGenericTypeDefinition() != ClassType)
                    return false;
            }
            else if (!ClassType.IsAssignableFrom(testClassType))
            {
                return false;
            }
            
            // Check generic arity if specified
            if (ClassGenericArity > 0)
            {
                var testGenericArgs = testClassType.IsGenericType 
                    ? testClassType.GetGenericArguments().Length 
                    : 0;
                if (testGenericArgs != ClassGenericArity)
                    return false;
            }
        }
        else if (dependentTest != null)
        {
            // If no ClassType specified, assume same class as dependent test
            if (test.TestClassType != dependentTest.TestClassType)
                return false;
        }
        
        // If MethodName is specified, it must match
        if (!string.IsNullOrEmpty(MethodName))
        {
            if (test.TestMethodName != MethodName)
                return false;
                
            // Check method parameters if specified
            if (MethodParameters != null)
            {
                var testParams = test.TestMethodParameterTypes ?? Array.Empty<string>();
                if (testParams.Length != MethodParameters.Length)
                    return false;
                    
                // Compare parameter types
                for (int i = 0; i < MethodParameters.Length; i++)
                {
                    if (testParams[i] != MethodParameters[i].FullName)
                        return false;
                }
            }
            
            // Check method generic arity if specified
            if (MethodGenericArity > 0)
            {
                // This would need to be added to TestMetadata if we want to support it
                // For now, we'll skip this check
            }
        }
        
        // Don't match self-dependencies when only class is specified
        if (ClassType != null && string.IsNullOrEmpty(MethodName) && dependentTest != null)
        {
            // If depending on all tests in a class, exclude self
            if (test.TestId == dependentTest.TestId)
                return false;
        }
        
        return true;
    }
    
    public bool Equals(TestDependency? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return ClassType == other.ClassType &&
               ClassGenericArity == other.ClassGenericArity &&
               MethodName == other.MethodName &&
               MethodGenericArity == other.MethodGenericArity &&
               (MethodParameters?.SequenceEqual(other.MethodParameters ?? Array.Empty<Type>()) ?? 
                other.MethodParameters == null);
    }
    
    public override bool Equals(object? obj) => Equals(obj as TestDependency);
    
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (ClassType?.GetHashCode() ?? 0);
            hash = hash * 31 + ClassGenericArity.GetHashCode();
            hash = hash * 31 + (MethodName?.GetHashCode() ?? 0);
            hash = hash * 31 + MethodGenericArity.GetHashCode();
            if (MethodParameters != null)
            {
                foreach (var param in MethodParameters)
                    hash = hash * 31 + (param?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
    
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if (ClassType != null)
        {
            parts.Add($"Class={ClassType.Name}");
            if (ClassGenericArity > 0)
                parts.Add($"ClassGenericArity={ClassGenericArity}");
        }
        
        if (!string.IsNullOrEmpty(MethodName))
        {
            parts.Add($"Method={MethodName}");
            if (MethodGenericArity > 0)
                parts.Add($"MethodGenericArity={MethodGenericArity}");
            if (MethodParameters?.Length > 0)
                parts.Add($"Params=[{string.Join(", ", MethodParameters.Select(p => p.Name))}]");
        }
        
        return $"TestDependency({string.Join(", ", parts)})";
    }
}