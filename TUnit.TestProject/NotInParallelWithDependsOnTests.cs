﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class NotInParallelWithDependsOnTestsBaseClass;

[ClassConstructor<NotInParallelWithDependsOnTestsDummyClassConstructor>]

[NotInParallel]
public class NotInParallelWithDependsOnTests : NotInParallelWithDependsOnTestsBaseClass
{
    [Test]
    public void Step1()
    {
        throw new Exception("Whoops!");
    }

    [Test]
    [DependsOn(nameof(Step1))]
    public void Step2()
    {
    }

    [Test]
    [DependsOn(nameof(Step2))]
    public void Step3()
    {
    }
}

public class NotInParallelWithDependsOnTestsDummyClassConstructor : IClassConstructor
{
    public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ClassConstructorMetadata classConstructorMetadata) where T : class
    {
        if (typeof(T) != typeof(NotInParallelWithDependsOnTests))
        {
            throw new Exception("Unhandled.");
        }

        return (T)(object)new NotInParallelWithDependsOnTests();
    }
}