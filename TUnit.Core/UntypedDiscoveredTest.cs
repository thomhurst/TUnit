using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2060:Call to \'System.Reflection.MethodInfo.MakeGenericMethod\' can not be statically analyzed. It\'s not possible to guarantee the availability of requirements of the generic method.")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
internal record UntypedDiscoveredTest(ResettableLazy<object> ResettableLazy) : DiscoveredTest
{
    public override async ValueTask ExecuteTest(CancellationToken cancellationToken)
    {
        TestContext.CancellationToken = cancellationToken;

        var arguments = TestDetails.TestMethodArguments.ToList();

        if (TestDetails.TestMethod.Parameters.Any(x => x.Type == typeof(CancellationToken)))
        {
            arguments.Add(cancellationToken);
        }

        try
        {
            await TestExecutor.ExecuteTest(TestContext, () =>
            {
                return AsyncConvert.ConvertObject(TestDetails.TestMethod.ReflectionInformation.Invoke(ResettableLazy.Value,
                    arguments.Select((x, i) => CastHelper.Cast(TestDetails.TestMethod.Parameters[i].Type, x)).ToArray()));
            });
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException ?? targetInvocationException).Throw();
            }

            throw;
        }
    }

    public override ValueTask ResetTestInstance()
    {
        return ResettableLazy.ResetLazy();
    }

    public override IClassConstructor? ClassConstructor
    {
        get;
    } = ResettableLazy.ClassConstructor;
}
