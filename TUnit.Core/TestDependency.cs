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

            if (ClassType.IsGenericTypeDefinition && testClassType.IsGenericType)
            {
                if (testClassType.GetGenericTypeDefinition() != ClassType)
                {
                    var currentType = testClassType.BaseType;
                    var found = false;
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
                }
            }
            else if (!ClassType.IsAssignableFrom(testClassType))
            {
                return false;
            }

            if (ClassGenericArity > 0)
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
            if (test.TestClassType != dependentTest.TestClassType)
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

            if (MethodParameters != null)
            {
                var testParams = test.TestMethodParameterTypes ?? [
                ];
                if (testParams.Length != MethodParameters.Length)
                {
                    return false;
                }

                for (var i = 0; i < MethodParameters.Length; i++)
                {
                    if (testParams[i] != MethodParameters[i].FullName)
                    {
                        return false;
                    }
                }
            }

            if (MethodGenericArity > 0)
            {
                // This would need to be added to TestMetadata if we want to support it
                // For now, we'll skip this check
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
               (MethodParameters?.SequenceEqual(other.MethodParameters ?? [
                   ]) ??
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
