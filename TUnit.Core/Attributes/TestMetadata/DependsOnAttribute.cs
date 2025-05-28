namespace TUnit.Core;

/// <summary>
/// Generic version of the <see cref="DependsOnAttribute"/> that specifies a dependency on a test method in a specific class.
/// </summary>
/// <typeparam name="T">The type that contains the test method this test depends on.</typeparam>
/// <remarks>
/// This generic version simplifies specifying dependencies with strong typing, avoiding the need to use <see cref="Type"/> parameters directly.
/// </remarks>
/// <example>
/// <code>
/// // The FeatureTest will only run if UserLoginTest passes
/// [Test]
/// [DependsOn&lt;LoginTests&gt;("UserLoginTest")]
/// public void FeatureTest()
/// {
///     // This test will only run if UserLoginTest in the LoginTests class has passed
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute<T> : DependsOnAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute{T}"/> class.
    /// Specifies a dependency on all test methods in the specified class.
    /// </summary>
    public DependsOnAttribute() : base(typeof(T))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute{T}"/> class.
    /// Specifies a dependency on a specific test method in the specified class.
    /// </summary>
    /// <param name="testName">The name of the test method that must run successfully before this test.</param>
    public DependsOnAttribute(string testName) : base(typeof(T), testName)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute{T}"/> class.
    /// Specifies a dependency on a specific test method with parameter types in the specified class.
    /// </summary>
    /// <param name="testName">The name of the test method that must run successfully before this test.</param>
    /// <param name="parameterTypes">The parameter types of the test method, used to disambiguate overloaded methods.</param>
    public DependsOnAttribute(string testName, Type[] parameterTypes) : base(typeof(T), testName, parameterTypes)
    {
    }
}

/// <summary>
/// Specifies that a test method or class has dependencies on other test methods that must run successfully before this test can execute.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute when you have tests that depend on the successful execution of other tests.
/// A test decorated with this attribute will only run if all its dependencies have passed.
/// </para>
/// 
/// <para>
/// Dependencies can be specified by:
/// </para>
/// <list type="bullet">
/// <item>Test method name (when depending on a test in the same class)</item>
/// <item>Test class type (when depending on all tests in another class)</item>
/// <item>Combination of test class type and method name (when depending on a specific test in another class)</item>
/// <item>Test method name and parameter types (when depending on a specific overloaded method)</item>
/// </list>
/// 
/// <para>
/// For better type safety, use the generic version <see cref="DependsOnAttribute{T}"/> when specifying dependencies on tests in other classes.
/// </para>
/// 
/// <para>
/// When <see cref="ProceedOnFailure"/> is set to true, the test will run even if its dependencies failed.
/// By default, a test will be skipped if any of its dependencies failed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple dependency on a test in the same class
/// [Test]
/// public void FirstTest() { }
/// 
/// [Test]
/// [DependsOn("FirstTest")]
/// public void SecondTest() 
/// {
///     // This test will only run if FirstTest passes
/// }
/// 
/// // Dependency on a test in another class
/// [Test]
/// [DependsOn(typeof(SetupTests), "Initialize")]
/// public void TestRequiringSetup() 
/// {
///     // This test will only run if the Initialize test in SetupTests passes
/// }
/// 
/// // Dependency with overloaded method disambiguation
/// [Test]
/// [DependsOn("OverloadedTest", new Type[] { typeof(string), typeof(int) })]
/// public void TestWithOverloadDependency() { }
/// 
/// // Proceeding even if dependency fails
/// [Test]
/// [DependsOn("CriticalTest") { ProceedOnFailure = true }]
/// public void TestThatRunsAnyway() 
/// {
///     // This test will run even if CriticalTest fails
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : TUnitAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
    /// Specifies a dependency on a test method in the same class.
    /// </summary>
    /// <param name="testName">The name of the test method that must run successfully before this test.</param>
    public DependsOnAttribute(string testName) : this(testName, null!)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
    /// Specifies a dependency on an overloaded test method in the same class.
    /// </summary>
    /// <param name="testName">The name of the test method that must run successfully before this test.</param>
    /// <param name="parameterTypes">The parameter types of the test method, used to disambiguate overloaded methods.</param>
    public DependsOnAttribute(string testName, Type[] parameterTypes) : this(null!, testName, parameterTypes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
    /// Specifies a dependency on all test methods in a specific class.
    /// </summary>
    /// <param name="testClass">The class containing the test methods that must run successfully before this test.</param>
    public DependsOnAttribute(Type testClass) : this(testClass, null!)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
    /// Specifies a dependency on a specific test method in a specific class.
    /// </summary>
    /// <param name="testClass">The class containing the test method that must run successfully before this test.</param>
    /// <param name="testName">The name of the test method that must run successfully before this test.</param>
    public DependsOnAttribute(Type testClass, string testName) : this(testClass, testName, null!)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
    /// Specifies a dependency on a specific overloaded test method in a specific class.
    /// </summary>
    /// <param name="testClass">The class containing the test method that must run successfully before this test.</param>
    /// <param name="testName">The name of the test method that must run successfully before this test.</param>
    /// <param name="parameterTypes">The parameter types of the test method, used to disambiguate overloaded methods.</param>
    public DependsOnAttribute(Type testClass, string testName, Type[] parameterTypes)
    {
        TestClass = testClass;
        TestName = testName;
        ParameterTypes = parameterTypes;
    }
    
    /// <summary>
    /// Gets the class containing the test method this test depends on.
    /// </summary>
    /// <remarks>
    /// If null, the dependency is assumed to be on a test in the same class.
    /// </remarks>
    public Type? TestClass { get; }

    /// <summary>
    /// Gets the name of the test method this test depends on.
    /// </summary>
    /// <remarks>
    /// If null, the dependency is assumed to be on all tests in the <see cref="TestClass"/>.
    /// </remarks>
    public string? TestName { get; }

    /// <summary>
    /// Gets the parameter types of the test method this test depends on.
    /// </summary>
    /// <remarks>
    /// Used to disambiguate overloaded methods with the same name.
    /// If null, the first method matching <see cref="TestName"/> will be used.
    /// </remarks>
    public Type[]? ParameterTypes { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this test should proceed even if its dependencies have failed.
    /// </summary>
    /// <remarks>
    /// When set to true, the test will run even if its dependencies failed.
    /// When set to false (default), the test will be skipped if any of its dependencies failed.
    /// </remarks>
    public bool ProceedOnFailure { get; set; }

    /// <summary>
    /// Returns a string representation of the dependency.
    /// </summary>
    /// <returns>A string that represents the dependency.</returns>
    public override string ToString()
    {
        if (TestClass != null && TestName == null)
        {
            return TestClass.Name;
        }

        if (ParameterTypes is { Length: > 0 })
        {
            return $"{TestName}({string.Join(", ", ParameterTypes.SelectMany(x => x.Name))}";
        }

        return TestName!;
    }
}