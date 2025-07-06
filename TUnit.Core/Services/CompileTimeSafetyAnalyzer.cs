using System.Reflection;
using TUnit.Core.Extensions;

namespace TUnit.Core.Services;

/// <summary>
/// Analyzes test methods and data sources for compile-time safety in AOT scenarios.
/// Identifies patterns that can't be resolved at compile-time and suggests alternatives.
/// </summary>
public class CompileTimeSafetyAnalyzer
{
    /// <summary>
    /// Analyzes a test method for compile-time safety issues.
    /// </summary>
    /// <param name="methodMetadata">The method to analyze</param>
    /// <returns>Analysis results with safety assessment and recommendations</returns>
    public CompileTimeSafetyAnalysis AnalyzeMethod(MethodMetadata methodMetadata)
    {
        var issues = new List<CompileTimeSafetyIssue>();
        var recommendations = new List<string>();

        // Check method signature
        AnalyzeMethodSignature(methodMetadata, issues, recommendations);

        // Check data attributes
        AnalyzeDataAttributes(methodMetadata, issues, recommendations);

        // Check generic constraints
        AnalyzeGenericConstraints(methodMetadata, issues, recommendations);

        // Check return type
        AnalyzeReturnType(methodMetadata, issues, recommendations);

        var isSafe = issues.All(i => i.Severity != SafetyIssueSeverity.Error);

        return new CompileTimeSafetyAnalysis
        {
            IsSafe = isSafe,
            Issues = issues.AsReadOnly(),
            Recommendations = recommendations.AsReadOnly(),
            MethodMetadata = methodMetadata
        };
    }

    /// <summary>
    /// Analyzes a test class for compile-time safety issues.
    /// </summary>
    /// <param name="classMetadata">The class to analyze</param>
    /// <returns>Analysis results with safety assessment and recommendations</returns>
    public CompileTimeSafetyAnalysis AnalyzeClass(ClassMetadata classMetadata)
    {
        var issues = new List<CompileTimeSafetyIssue>();
        var recommendations = new List<string>();

        // Check class constructors
        AnalyzeClassConstructors(classMetadata, issues, recommendations);

        // Check class data attributes
        AnalyzeClassDataAttributes(classMetadata, issues, recommendations);

        // Check property injection
        AnalyzePropertyInjection(classMetadata, issues, recommendations);

        var isSafe = issues.All(i => i.Severity != SafetyIssueSeverity.Error);

        return new CompileTimeSafetyAnalysis
        {
            IsSafe = isSafe,
            Issues = issues.AsReadOnly(),
            Recommendations = recommendations.AsReadOnly(),
            ClassMetadata = classMetadata
        };
    }

    private static void AnalyzeMethodSignature(MethodMetadata methodMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        // Check for complex parameter types
        foreach (var parameter in methodMetadata.Parameters)
        {
            if (IsComplexType(parameter.Type))
            {
                issues.Add(new CompileTimeSafetyIssue
                {
                    Severity = SafetyIssueSeverity.Warning,
                    Message = $"Parameter '{parameter.Name}' has complex type '{parameter.Type.Name}' that may not be AOT-safe",
                    Location = $"{methodMetadata.DeclaringType().Name}.{methodMetadata.MethodName()}",
                    IssueType = SafetyIssueType.ComplexParameterType
                });

                recommendations.Add($"Consider using simpler types for parameter '{parameter.Name}' or ensure proper AOT configuration");
            }
        }

        // Check for generic method parameters
        if (methodMetadata.IsGenericMethodDefinition())
        {
            issues.Add(new CompileTimeSafetyIssue
            {
                Severity = SafetyIssueSeverity.Warning,
                Message = "Generic test methods may have limited AOT support",
                Location = $"{methodMetadata.DeclaringType().Name}.{methodMetadata.MethodName()}",
                IssueType = SafetyIssueType.GenericMethod
            });

            recommendations.Add("Consider using non-generic test methods with concrete types for better AOT compatibility");
        }
    }

    private static void AnalyzeDataAttributes(MethodMetadata methodMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        var dataAttributes = methodMetadata.GetDataAttributes();

        foreach (var dataAttribute in dataAttributes)
        {
            if (IsRuntimeOnlyDataAttribute(dataAttribute))
            {
                issues.Add(new CompileTimeSafetyIssue
                {
                    Severity = SafetyIssueSeverity.Error,
                    Message = $"Data attribute '{dataAttribute.GetType().Name}' requires runtime evaluation and is not AOT-safe",
                    Location = $"{methodMetadata.DeclaringType().Name}.{methodMetadata.MethodName()}",
                    IssueType = SafetyIssueType.RuntimeDataAttribute
                });

                recommendations.Add($"Replace '{dataAttribute.GetType().Name}' with compile-time resolvable data sources like ArgumentsAttribute");
            }
        }
    }

    private static void AnalyzeGenericConstraints(MethodMetadata methodMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        if (methodMetadata.DeclaringType().IsGenericType)
        {
            issues.Add(new CompileTimeSafetyIssue
            {
                Severity = SafetyIssueSeverity.Info,
                Message = "Generic test classes require careful AOT configuration",
                Location = methodMetadata.DeclaringType().Name,
                IssueType = SafetyIssueType.GenericClass
            });

            recommendations.Add("Ensure all generic type arguments are properly configured for AOT compilation");
        }
    }

    private static void AnalyzeReturnType(MethodMetadata methodMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        var returnType = methodMetadata.ReturnType;
        if (returnType != null && IsComplexType(returnType))
        {
            issues.Add(new CompileTimeSafetyIssue
            {
                Severity = SafetyIssueSeverity.Info,
                Message = $"Complex return type '{returnType.Name}' may require AOT configuration",
                Location = $"{methodMetadata.DeclaringType().Name}.{methodMetadata.MethodName()}",
                IssueType = SafetyIssueType.ComplexReturnType
            });

            recommendations.Add($"Ensure return type '{returnType.Name}' is properly configured for AOT compilation");
        }
    }

    private static void AnalyzeClassConstructors(ClassMetadata classMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        var constructors = classMetadata.Type.GetConstructors();

        if (constructors.Length > 1)
        {
            issues.Add(new CompileTimeSafetyIssue
            {
                Severity = SafetyIssueSeverity.Warning,
                Message = "Multiple constructors may complicate AOT instance creation",
                Location = classMetadata.Type.Name,
                IssueType = SafetyIssueType.MultipleConstructors
            });

            recommendations.Add("Consider using a single constructor or ensure proper constructor selection for AOT scenarios");
        }

        foreach (var constructor in constructors)
        {
            if (constructor.GetParameters().Any(p => IsComplexType(p.ParameterType)))
            {
                issues.Add(new CompileTimeSafetyIssue
                {
                    Severity = SafetyIssueSeverity.Warning,
                    Message = "Constructor has complex parameter types that may not be AOT-safe",
                    Location = classMetadata.Type.Name,
                    IssueType = SafetyIssueType.ComplexConstructorParameters
                });

                recommendations.Add("Consider simplifying constructor parameters for better AOT compatibility");
            }
        }
    }

    private static void AnalyzeClassDataAttributes(ClassMetadata classMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        var dataAttributes = classMetadata.GetDataAttributes();

        foreach (var dataAttribute in dataAttributes)
        {
            if (IsRuntimeOnlyDataAttribute(dataAttribute))
            {
                issues.Add(new CompileTimeSafetyIssue
                {
                    Severity = SafetyIssueSeverity.Error,
                    Message = $"Class-level data attribute '{dataAttribute.GetType().Name}' requires runtime evaluation",
                    Location = classMetadata.Type.Name,
                    IssueType = SafetyIssueType.RuntimeDataAttribute
                });

                recommendations.Add($"Replace class-level '{dataAttribute.GetType().Name}' with compile-time resolvable alternatives");
            }
        }
    }

    private static void AnalyzePropertyInjection(ClassMetadata classMetadata, List<CompileTimeSafetyIssue> issues, List<string> recommendations)
    {
        var properties = classMetadata.Type.GetProperties();
        var injectableProperties = properties.Where(p => HasPropertyInjectionAttribute(p)).ToList();

        if (injectableProperties.Any())
        {
            issues.Add(new CompileTimeSafetyIssue
            {
                Severity = SafetyIssueSeverity.Warning,
                Message = "Property injection may require runtime configuration for AOT scenarios",
                Location = classMetadata.Type.Name,
                IssueType = SafetyIssueType.PropertyInjection
            });

            recommendations.Add("Ensure property injection sources are AOT-compatible or use constructor injection instead");
        }
    }

    private static bool IsComplexType(Type type)
    {
        return type.IsGenericType ||
               type.IsInterface ||
               type.IsAbstract ||
               type.Assembly != typeof(string).Assembly; // Not in core library
    }

    private static bool IsRuntimeOnlyDataAttribute(IDataAttribute dataAttribute)
    {
        return dataAttribute switch
        {
            MethodDataSourceAttribute => true,
            _ when dataAttribute.GetType().Name.Contains("Dynamic") => true,
            _ when dataAttribute.GetType().Name.Contains("Runtime") => true,
            _ => false
        };
    }

    private static bool HasPropertyInjectionAttribute(PropertyInfo property)
    {
        // Check for common property injection attributes
        return property.GetCustomAttributes().Any(attr =>
            attr.GetType().Name.Contains("Inject") ||
            attr.GetType().Name.Contains("Property"));
    }
}

/// <summary>
/// Results of compile-time safety analysis.
/// </summary>
public sealed class CompileTimeSafetyAnalysis
{
    /// <summary>
    /// Whether the analyzed code is considered AOT-safe.
    /// </summary>
    public bool IsSafe { get; init; }

    /// <summary>
    /// Issues found during analysis.
    /// </summary>
    public IReadOnlyList<CompileTimeSafetyIssue> Issues { get; init; } = [
    ];

    /// <summary>
    /// Recommendations for improving AOT safety.
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; } = [
    ];

    /// <summary>
    /// The method that was analyzed (if applicable).
    /// </summary>
    public MethodMetadata? MethodMetadata { get; init; }

    /// <summary>
    /// The class that was analyzed (if applicable).
    /// </summary>
    public ClassMetadata? ClassMetadata { get; init; }
}

/// <summary>
/// A specific compile-time safety issue.
/// </summary>
public sealed class CompileTimeSafetyIssue
{
    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public SafetyIssueSeverity Severity { get; init; }

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Location where the issue was found.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Type/category of the issue.
    /// </summary>
    public SafetyIssueType IssueType { get; init; }
}

/// <summary>
/// Severity levels for compile-time safety issues.
/// </summary>
public enum SafetyIssueSeverity
{
    /// <summary>
    /// Informational issue - no action required but good to know.
    /// </summary>
    Info,

    /// <summary>
    /// Warning - may cause issues in AOT scenarios.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - will definitely cause issues in AOT scenarios.
    /// </summary>
    Error
}

/// <summary>
/// Types of compile-time safety issues.
/// </summary>
public enum SafetyIssueType
{
    /// <summary>
    /// Complex parameter type that may not be AOT-safe.
    /// </summary>
    ComplexParameterType,

    /// <summary>
    /// Generic method that may have limited AOT support.
    /// </summary>
    GenericMethod,

    /// <summary>
    /// Generic class that requires AOT configuration.
    /// </summary>
    GenericClass,

    /// <summary>
    /// Data attribute that requires runtime evaluation.
    /// </summary>
    RuntimeDataAttribute,

    /// <summary>
    /// Complex return type that may require AOT configuration.
    /// </summary>
    ComplexReturnType,

    /// <summary>
    /// Multiple constructors that may complicate AOT instance creation.
    /// </summary>
    MultipleConstructors,

    /// <summary>
    /// Complex constructor parameters that may not be AOT-safe.
    /// </summary>
    ComplexConstructorParameters,

    /// <summary>
    /// Property injection that may require runtime configuration.
    /// </summary>
    PropertyInjection
}
