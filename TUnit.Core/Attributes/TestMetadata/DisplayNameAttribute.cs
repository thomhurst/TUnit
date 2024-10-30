using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DisplayNameAttribute(string displayName) : TUnitAttribute, ITestDiscoveryEventReceiver
{
    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        var mutableDisplayName = displayName;
        
        var parameters = discoveredTestContext
            .TestDetails
            .MethodInfo
            .GetParameters()
            .Zip(discoveredTestContext.TestDetails.TestMethodArguments, (parameterInfo, testArgument) => (ParameterInfo: parameterInfo, TestArgument: testArgument));
        
        foreach (var parameter in parameters)
        {
            mutableDisplayName = mutableDisplayName.Replace($"${parameter.ParameterInfo.Name}",
                ArgumentFormatter.GetConstantValue(discoveredTestContext.TestContext, parameter.TestArgument));
        }

        discoveredTestContext.SetDisplayName(mutableDisplayName);
    }
}
