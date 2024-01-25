using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.Core.Attributes;

namespace TUnit.TestAdapter;

public class TestsLoader(IMessageLogger? messageLogger)
{
    private static readonly Type[] TestAttributes = [typeof(TestAttribute), typeof(TestWithDataAttribute)];
    private readonly SourceLocationHelper _sourceLocationHelper = new(messageLogger);

    public IEnumerable<Test> GetTests(TypeInformation typeInformation)
    {
        var methods = typeInformation.Types.SelectMany(x => x.GetMethods());

        foreach (var methodInfo in methods)
        {
            if (!HasTestAttributes(methodInfo))
            {
                continue;
            }
            
            var sourceLocation = _sourceLocationHelper
                .GetSourceLocation(typeInformation.Assembly.Location, methodInfo.DeclaringType!.FullName!, methodInfo.Name);
            
            foreach (var testWithDataAttribute in methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestWithDataAttribute)))
            {
                foreach (var customAttributeTypedArgument in testWithDataAttribute.ConstructorArguments)
                {
                    var arguments =
                        (customAttributeTypedArgument.Value as IEnumerable<CustomAttributeTypedArgument>)
                        ?.Select(x => new ParameterArgument(x.Value?.GetType()!, x.Value))
                        .ToArray();
                    
                    yield return new Test(
                            MethodInfo: methodInfo,
                            SourceLocation: sourceLocation,
                            arguments: arguments
                        );
                }
            }
            
            if(methodInfo.CustomAttributes.Any(x => x.AttributeType == typeof(TestAttribute)))
            {
                yield return new Test(
                    MethodInfo: methodInfo,
                    SourceLocation: sourceLocation,
                    arguments: null
                );
            }
        }
    }

    private static bool HasTestAttributes(MethodInfo methodInfo)
    {
        return methodInfo.CustomAttributes.Select(x => x.AttributeType).Intersect(TestAttributes).Any();
    }
}