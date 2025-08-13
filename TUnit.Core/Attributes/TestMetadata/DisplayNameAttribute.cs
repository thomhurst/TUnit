using TUnit.Core.Helpers;

#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

/// <summary>
/// Attribute that allows specifying a custom display name for a test method or test class.
/// </summary>
/// <remarks>
/// <para>
/// This attribute can be applied to test methods or test classes to provide more descriptive names than the default method or class name.
/// </para>
/// <para>
/// The display name can include parameter placeholders in the format of "$parameterName" which will be
/// replaced with the actual parameter values during test execution. For test methods, method parameters 
/// will be used for substitution. For test classes, constructor parameters will be used for substitution. For example:
/// <code>
/// [Test]
/// [Arguments("John", 25)]
/// [DisplayName("User $name is $age years old")]
/// public void TestUser(string name, int age) { ... }
/// 
/// [Arguments("TestData")]
/// [DisplayName("Class with data: $data")]
/// public class MyTestClass(string data) { ... }
/// </code>
/// </para>
/// <para>
/// When these tests run, the display names would appear as "User John is 25 years old" and 
/// "Class with data: TestData" respectively.
/// </para>
/// </remarks>
/// <param name="displayName">
/// The display name template. Can include parameter placeholders in the format of "$parameterName".
/// For methods, method parameter names can be referenced. For classes, constructor parameter names can be referenced.
/// </param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class DisplayNameAttribute(string displayName) : DisplayNameFormatterAttribute, IScopedAttribute<DisplayNameAttribute>
{
    /// <inheritdoc />
    protected override string FormatDisplayName(DiscoveredTestContext context)
    {
        var testDetails = context.TestDetails;

        var mutableDisplayName = displayName;

        // Try to substitute method parameters first
        var methodParameters = testDetails
            .MethodMetadata
            .Parameters
            .Zip(testDetails.TestMethodArguments, (parameterInfo, testArgument) => (ParameterInfo: parameterInfo, TestArgument: testArgument))
            .OrderByDescending(p => p.ParameterInfo.Name?.Length ?? 0); // Sort by name length descending to avoid prefix issues

        foreach (var parameter in methodParameters)
        {
            mutableDisplayName = mutableDisplayName.Replace($"${parameter.ParameterInfo.Name}",
                ArgumentFormatter.Format(parameter.TestArgument, context.ArgumentDisplayFormatters));
        }

        // If there are still placeholders and we have class parameters, try to substitute them
        if (mutableDisplayName.Contains('$') && testDetails.TestClassArguments.Length > 0)
        {
            var classParameters = testDetails
                .MethodMetadata
                .Class
                .Parameters
                .Zip(testDetails.TestClassArguments, (parameterInfo, testArgument) => (ParameterInfo: parameterInfo, TestArgument: testArgument))
                .OrderByDescending(p => p.ParameterInfo.Name?.Length ?? 0); // Sort by name length descending to avoid prefix issues

            foreach (var parameter in classParameters)
            {
                mutableDisplayName = mutableDisplayName.Replace($"${parameter.ParameterInfo.Name}",
                    ArgumentFormatter.Format(parameter.TestArgument, context.ArgumentDisplayFormatters));
            }
        }

        return mutableDisplayName;
    }
}
