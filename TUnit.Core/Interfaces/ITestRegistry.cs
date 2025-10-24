﻿using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for registering and managing dynamically added tests during runtime execution.
/// </summary>
public interface ITestRegistry
{
    /// <summary>
    /// Adds a dynamic test to be executed during the current test session.
    /// </summary>
    /// <typeparam name="T">The test class type</typeparam>
    /// <param name="context">The current test context</param>
    /// <param name="dynamicTest">The dynamic test instance to add</param>
    /// <returns>A task that completes when the test has been queued for execution</returns>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Adding dynamic tests requires runtime compilation and reflection which are not supported in native AOT scenarios.")]
    #endif
    Task AddDynamicTest<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(TestContext context, DynamicTest<T> dynamicTest)
        where T : class;
}
