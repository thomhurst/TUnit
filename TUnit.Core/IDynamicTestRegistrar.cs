using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public interface IDynamicTestRegistrar
{
    Task Register<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                    | DynamicallyAccessedMemberTypes.PublicMethods
                                    | DynamicallyAccessedMemberTypes.PublicProperties)]
        TClass>(DynamicTest<TClass> dynamicTest) where TClass : class;
}