namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for types that require asynchronous initialization during test discovery.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="IAsyncInitializer"/> which runs during test execution,
/// implementations of this interface are initialized during the test discovery phase.
/// This enables data sources (such as <c>InstanceMethodDataSource</c>) to access
/// fully-initialized objects when generating test cases.
/// </para>
/// <para>
/// Common use cases include:
/// <list type="bullet">
/// <item><description>Starting Docker containers before test case enumeration</description></item>
/// <item><description>Connecting to databases to discover parameterized test data</description></item>
/// <item><description>Initializing fixtures that provide data for test case generation</description></item>
/// </list>
/// </para>
/// <para>
/// This interface extends <see cref="IAsyncInitializer"/>, meaning the same
/// <see cref="IAsyncInitializer.InitializeAsync"/> method is used. The framework
/// guarantees exactly-once initialization semantics - objects will not be
/// re-initialized during test execution.
/// </para>
/// </remarks>
public interface IAsyncDiscoveryInitializer : IAsyncInitializer;
