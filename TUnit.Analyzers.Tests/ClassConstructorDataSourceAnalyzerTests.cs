using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ClassParametersAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ClassConstructorDataSourceAnalyzerTests
{
    [Test]
    public async Task Class_Attribute_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using TUnit.Core;
                using TUnit.Core.Interfaces;

                [ClassConstructor<MyClassConstructor>]
                public class MyClass
                {
                    public MyClass(int value)
                    {
                    }
                            
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyClassConstructor : IClassConstructor
                {
                    public object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
                    {
                        return Activator.CreateInstance(type, 1)!;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Assembly_Attribute_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Diagnostics.CodeAnalysis;
                using TUnit.Core;
                using TUnit.Core.Interfaces;

                [assembly: ClassConstructor<MyClassConstructor>]
                    
                public class MyClass
                {
                    public MyClass(int value)
                    {
                    }
                            
                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyClassConstructor : IClassConstructor
                {
                    public object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
                    {
                        return Activator.CreateInstance(type, 1)!;
                    }
                }
                """
            );
    }
}
