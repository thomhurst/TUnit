using TUnit.Core.Helpers;

#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

/// <summary>
/// Attribute that allows specifying a custom display name for a test method.
/// </summary>
/// <remarks>
/// <para>
/// This attribute can be applied to test methods to provide more descriptive names than the default method name.
/// </para>
/// <para>
/// The display name can include parameter placeholders in the format of "$parameterName" which will be 
/// replaced with the actual parameter values during test execution. For example:
/// <code>
/// [Test]
/// [Arguments("John", 25)]
/// [DisplayName("User $name is $age years old")]
/// public void TestUser(string name, int age) { ... }
/// </code>
/// </para>
/// <para>
/// When this test runs, the display name would appear as "User John is 25 years old".
/// </para>
/// </remarks>
/// <param name="displayName">
/// The display name template. Can include parameter placeholders in the format of "$parameterName".
/// </param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DisplayNameAttribute(string displayName) : DisplayNameFormatterAttribute
{
    /// <inheritdoc />
    protected override string FormatDisplayName(TestContext testContext)
    {
        var testDetails = testContext.TestDetails;
        
        var mutableDisplayName = displayName;
        
        var parameters = testDetails
            .TestMethod
            .Parameters
            .Zip(testDetails.TestMethodArguments, (parameterInfo, testArgument) => (ParameterInfo: parameterInfo, TestArgument: testArgument));
        
        foreach (var parameter in parameters)
        {
            mutableDisplayName = mutableDisplayName.Replace($"${parameter.ParameterInfo.Name}",
                ArgumentFormatter.GetConstantValue(testContext, parameter.TestArgument));
        }

        return mutableDisplayName;
    }
}
