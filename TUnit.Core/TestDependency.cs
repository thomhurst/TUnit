namespace TUnit.Core;

/// <summary>
/// Represents a dependency on another test with support for generic types and methods
/// </summary>
public sealed class TestDependency : IEquatable<TestDependency>
{
    public Type? ClassType { get; init; }

    public int ClassGenericArity { get; init; }

    public string? MethodName { get; init; }

    public Type[]? MethodParameters { get; init; }

    public int MethodGenericArity { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this test should proceed even if its dependencies have failed.
    /// When set to false (default), the test will be skipped if any of its dependencies failed.
    /// When set to true, the test will run even if its dependencies failed.
    /// </summary>
    public bool ProceedOnFailure { get; init; }

    public static TestDependency FromMethodName(string methodName, bool proceedOnFailure = false)
    {
        return new TestDependency
        {
            MethodName = methodName,
            ProceedOnFailure = proceedOnFailure
        };
    }

    public static TestDependency FromClass(Type classType, bool proceedOnFailure = false)
    {
        return new TestDependency
        {
            ClassType = classType,
            ClassGenericArity = classType.IsGenericTypeDefinition ? classType.GetGenericArguments().Length : 0,
            ProceedOnFailure = proceedOnFailure
        };
    }

    public static TestDependency FromClassAndMethod(Type classType, string methodName, bool proceedOnFailure = false)
    {
        return new TestDependency
        {
            ClassType = classType,
            ClassGenericArity = classType.IsGenericTypeDefinition ? classType.GetGenericArguments().Length : 0,
            MethodName = methodName,
            ProceedOnFailure = proceedOnFailure
        };
    }

    public bool Matches(TestMetadata test, TestMetadata? dependentTest = null)
    {
        if (ClassType != null)
        {
            var testClassType = test.TestClassType;

            if (ClassType.IsGenericTypeDefinition)
            {
                // Check if the test class type or any of its base types match the generic type definition
                var found = false;
                var currentType = testClassType;
                
                while (currentType != null && !found)
                {
                    if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == ClassType)
                    {
                        found = true;
                    }
                    currentType = currentType.BaseType;
                }
                
                if (!found)
                {
                    return false;
                }
                
                // For generic type definitions, we don't need to check arity against the test class
                // because the test class may inherit from a closed generic version of the dependency
            }
            else if (!ClassType.IsAssignableFrom(testClassType))
            {
                return false;
            }

            if (ClassGenericArity > 0 && !ClassType.IsGenericTypeDefinition)
            {
                var testGenericArgs = testClassType.IsGenericType
                    ? testClassType.GetGenericArguments().Length
                    : 0;
                if (testGenericArgs != ClassGenericArity)
                {
                    return false;
                }
            }
        }
        else if (dependentTest != null)
        {
            var testType = test.TestClassType;
            var dependentType = dependentTest.TestClassType;
            
            if (testType != dependentType)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            if (test.TestMethodName != MethodName)
            {
                return false;
            }

            if(test.MethodMetadata.GenericTypeCount != MethodGenericArity)
            {
                return false;
            }

            if (MethodParameters != null)
            {
                var testParams = test.MethodMetadata.Parameters;

                if (testParams.Length != MethodParameters.Length
                    || !testParams.Select(x => x.Type).SequenceEqual(MethodParameters!))
                {
                    return false;
                }
            }
        }

        if (ClassType != null && string.IsNullOrEmpty(MethodName) && dependentTest != null)
        {
            if (test.TestClassType == dependentTest.TestClassType &&
                test.TestMethodName == dependentTest.TestMethodName)
            {
                return false;
            }
        }

        return true;
    }

    public bool Equals(TestDependency? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ClassType == other.ClassType &&
               ClassGenericArity == other.ClassGenericArity &&
               MethodName == other.MethodName &&
               MethodGenericArity == other.MethodGenericArity &&
               ProceedOnFailure == other.ProceedOnFailure &&
               (MethodParameters?.SequenceEqual(other.MethodParameters ?? []) ??
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
            hash = hash * 31 + ProceedOnFailure.GetHashCode();
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
        var parts = new List<string>();

        if (ClassType != null)
        {
            parts.Add($"Class={ClassType.Name}");
            if (ClassGenericArity > 0)
            {
                parts.Add($"ClassGenericArity={ClassGenericArity}");
            }
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            parts.Add($"Method={MethodName}");
            if (MethodGenericArity > 0)
            {
                parts.Add($"MethodGenericArity={MethodGenericArity}");
            }
            if (MethodParameters?.Length > 0)
            {
                parts.Add($"Params=[{string.Join(", ", MethodParameters.Select(p => p.Name))}]");
            }
        }

        if (ProceedOnFailure)
        {
            parts.Add("ProceedOnFailure=true");
        }

        return $"TestDependency({string.Join(", ", parts)})";
    }
}
