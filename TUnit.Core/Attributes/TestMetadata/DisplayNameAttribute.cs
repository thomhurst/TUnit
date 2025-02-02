using TUnit.Core.Helpers;

#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DisplayNameAttribute(string displayName) : DisplayNameFormatterAttribute
{
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
