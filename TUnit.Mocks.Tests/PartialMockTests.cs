using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Test classes used for partial mock tests.
/// </summary>
public abstract class AbstractService
{
    public abstract string GetName();
    public virtual int Calculate(int x) => x * 2;
    public string NonVirtualMethod() => "fixed";
}

public class ConcreteService
{
    public virtual string Greet(string name) => $"Hello, {name}!";
    public virtual int Add(int a, int b) => a + b;
}

/// <summary>Simulates Azure SDK patterns like BlobBaseClient/BlobClient.</summary>
public class BaseClientWithVirtuals
{
    public virtual string WithSnapshot(string snapshot) => $"base-{snapshot}";
    public virtual string WithVersion(string version) => $"base-{version}";
    public virtual int Calculate(int x) => x * 2;
    public virtual string Name { get; set; } = "base";
    public virtual int Priority { get; set; }
}

public class DerivedClientWithNewMethods : BaseClientWithVirtuals
{
    public new string WithSnapshot(string snapshot) => $"derived-{snapshot}";
    public new string WithVersion(string version) => $"derived-{version}";
}

public class DerivedClientWithNewProperties : BaseClientWithVirtuals
{
    public new string Name { get; set; } = "derived";
}

public class MixedNewAndOverrideClient : BaseClientWithVirtuals
{
    public new string WithSnapshot(string snapshot) => $"new-{snapshot}";
    public override string WithVersion(string version) => $"override-{version}";
    public new string Name { get; set; } = "mixed";
    public override int Priority { get; set; }
}

public class MiddleClient : BaseClientWithVirtuals
{
    public override string WithSnapshot(string snapshot) => $"middle-{snapshot}";
}

public class GrandchildClient : MiddleClient
{
    public new string WithSnapshot(string snapshot) => $"grandchild-{snapshot}";
}

public class NewVirtualClient : BaseClientWithVirtuals
{
    public new virtual string WithSnapshot(string snapshot) => $"newvirtual-{snapshot}";
}

public class NewVirtualDerivedClient : NewVirtualClient
{
    public override string WithSnapshot(string snapshot) => $"nvderived-{snapshot}";
}

// Separate hierarchy for wrap-mode tests (Mock.Of and Mock.Wrap on the same type collide on hintName).
public class WrapBaseClient
{
    public virtual string Process(string input) => $"base-{input}";
    public virtual int Compute(int x) => x + 1;
}

public class WrapDerivedClientWithNew : WrapBaseClient
{
    public new string Process(string input) => $"derived-{input}";
}

public class ComplexBaseService
{
    public virtual string Execute(string command) => $"base-exec-{command}";
    public virtual string Execute(string command, int timeout) => $"base-exec-{command}-{timeout}";
    public virtual string Execute(string command, int timeout, bool retry) => $"base-exec-{command}-{timeout}-{retry}";
    public virtual int GetStatus() => 0;
    public virtual string Format(string value) => $"base-fmt-{value}";
    public virtual string Format(int value) => $"base-fmt-{value}";
    public virtual string Format(string value, string locale) => $"base-fmt-{value}-{locale}";
    public virtual string Name { get; set; } = "base";
    public virtual int Priority { get; set; }
    public virtual string Description { get; set; } = "base-desc";
    public virtual event EventHandler? StatusChanged;
    public virtual event EventHandler<string>? MessageReceived;
}

public class ComplexDerivedService : ComplexBaseService
{
    // 'new' hides some Execute overloads, others remain virtual from base
    public new string Execute(string command) => $"derived-exec-{command}";
    public new string Execute(string command, int timeout) => $"derived-exec-{command}-{timeout}";
    // Execute(string,int,bool) is NOT hidden — remains virtual from base

    // 'override' for GetStatus
    public override int GetStatus() => 1;

    // 'new' hides one Format overload, leaves others
    public new string Format(string value) => $"derived-fmt-{value}";
    // Format(int) and Format(string,string) remain virtual from base

    // 'new' hides property
    public new string Name { get; set; } = "derived";
    // Priority and Description remain virtual from base

    // 'new' hides event
    public new event EventHandler? StatusChanged;
    // MessageReceived remains virtual from base
}

public abstract class ServiceWithConstructor
{
    protected readonly string _prefix;

    protected ServiceWithConstructor(string prefix)
    {
        _prefix = prefix;
    }

    public abstract string Format(string value);
    public virtual string GetPrefix() => _prefix;
}

/// <summary>
/// US8 Integration Tests: Partial mocks of abstract and concrete classes.
/// </summary>
public class PartialMockTests
{
    [Test]
    public async Task Of_Abstract_Class_Creates_Instance()
    {
        // Arrange & Act
        var mock = AbstractService.Mock();

        // Assert
        await Assert.That(mock).IsNotNull();
        await Assert.That(mock.Object).IsNotNull();
    }

    [Test]
    public async Task Abstract_Method_Returns_Configured_Value()
    {
        // Arrange
        var mock = AbstractService.Mock();
        mock.GetName().Returns("TestName");

        // Act
        var result = mock.Object.GetName();

        // Assert
        await Assert.That(result).IsEqualTo("TestName");
    }

    [Test]
    public async Task Unconfigured_Virtual_Method_Calls_Base_Implementation()
    {
        // Arrange
        var mock = AbstractService.Mock();
        // GetName is abstract - configure it
        mock.GetName().Returns("TestName");
        // Calculate is virtual - do NOT configure it

        // Act
        var result = mock.Object.Calculate(5);

        // Assert - base implementation is x * 2
        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    public async Task Configured_Virtual_Method_Returns_Override_Instead_Of_Base()
    {
        // Arrange
        var mock = AbstractService.Mock();
        mock.GetName().Returns("TestName");
        mock.Calculate(Any()).Returns(42);

        // Act
        var result = mock.Object.Calculate(5);

        // Assert - should return configured value, not base (10)
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Concrete_Class_Partial_Mock_Calls_Base_When_Not_Configured()
    {
        // Arrange
        var mock = ConcreteService.Mock();

        // Act - no setup, should call base
        var result = mock.Object.Greet("World");

        // Assert - base implementation
        await Assert.That(result).IsEqualTo("Hello, World!");
    }

    [Test]
    public async Task Concrete_Class_Override_Returns_Configured_Value()
    {
        // Arrange
        var mock = ConcreteService.Mock();
        mock.Greet(Any()).Returns("Mocked!");

        // Act
        var result = mock.Object.Greet("World");

        // Assert
        await Assert.That(result).IsEqualTo("Mocked!");
    }

    [Test]
    public async Task Concrete_Class_Multiple_Methods_Mixed_Setup()
    {
        // Arrange
        var mock = ConcreteService.Mock();
        mock.Greet("Alice").Returns("Hi Alice!");
        // Don't setup Add - let it call base

        // Act
        var greetResult = mock.Object.Greet("Alice");
        var addResult = mock.Object.Add(3, 4);

        // Assert
        await Assert.That(greetResult).IsEqualTo("Hi Alice!");
        await Assert.That(addResult).IsEqualTo(7); // base: a + b
    }

    [Test]
    public async Task Constructor_Args_Passed_To_Base()
    {
        // Arrange
        var mock = ServiceWithConstructor.Mock("PREFIX");
        mock.Format(Any()).Returns("formatted");

        // Act - GetPrefix is virtual and unconfigured, so calls base which uses _prefix
        var prefix = mock.Object.GetPrefix();

        // Assert
        await Assert.That(prefix).IsEqualTo("PREFIX");
    }

    [Test]
    public async Task Constructor_Args_Abstract_Method_Override()
    {
        // Arrange
        var mock = ServiceWithConstructor.Mock("test");
        mock.Format("value").Returns("test:value");

        // Act
        var result = mock.Object.Format("value");

        // Assert
        await Assert.That(result).IsEqualTo("test:value");
    }

    [Test]
    public async Task Non_Virtual_Method_Still_Works()
    {
        // Arrange
        var mock = AbstractService.Mock();
        mock.GetName().Returns("Name");

        // Act - NonVirtualMethod is not virtual, so it runs the original implementation
        var result = mock.Object.NonVirtualMethod();

        // Assert
        await Assert.That(result).IsEqualTo("fixed");
    }

    [Test]
    public void Verify_Calls_On_Partial_Mock()
    {
        // Arrange
        var mock = ConcreteService.Mock();
        mock.Greet(Any()).Returns("Hi");

        // Act
        mock.Object.Greet("Alice");
        mock.Object.Greet("Bob");

        // Assert
        mock.Greet(Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public void Verify_Calls_On_Unconfigured_Virtual_Method()
    {
        // Arrange
        var mock = ConcreteService.Mock();

        // Act - unconfigured, calls base
        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);

        // Assert - calls should still be recorded even when calling base
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Partial_Mock_With_Strict_Behavior_Abstract_Method_Throws_Without_Setup()
    {
        // Arrange
        var mock = AbstractService.Mock(MockBehavior.Strict);

        // Act & Assert - abstract method with no setup in strict mode should throw
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            mock.Object.GetName();
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Partial_Mock_With_Strict_Behavior_Virtual_Method_Calls_Base()
    {
        // Arrange - strict mode, but virtual methods should still call base when unconfigured
        var mock = ConcreteService.Mock(MockBehavior.Strict);

        // Act - virtual method with no setup should call base (not throw)
        var result = mock.Object.Add(2, 3);

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }

    // ========================================================================
    // Tests for 'new' method hiding (GitHub issue #5326)
    // ========================================================================

    [Test]
    public async Task New_Method_Hiding__Non_Hidden_Virtual_Can_Be_Configured()
    {
        // DerivedClientWithNewMethods hides WithSnapshot/WithVersion but not Calculate
        var mock = DerivedClientWithNewMethods.Mock();
        mock.Calculate(Any()).Returns(42);

        var result = mock.Object.Calculate(5);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task New_Method_Hiding__Non_Hidden_Virtual_Falls_Back_To_Base()
    {
        var mock = DerivedClientWithNewMethods.Mock();

        // Calculate is virtual in base and not hidden — unconfigured should call base (x * 2)
        var result = mock.Object.Calculate(7);

        await Assert.That(result).IsEqualTo(14);
    }

    [Test]
    public async Task New_Method_Hiding__Strict_Mode_Works()
    {
        var mock = DerivedClientWithNewMethods.Mock(MockBehavior.Strict);
        mock.Calculate(Any()).Returns(99);

        var result = mock.Object.Calculate(10);

        await Assert.That(result).IsEqualTo(99);
    }

    [Test]
    public void New_Method_Hiding__Verify_Calls_On_Non_Hidden_Virtual()
    {
        var mock = DerivedClientWithNewMethods.Mock();

        mock.Object.Calculate(1);
        mock.Object.Calculate(2);

        mock.Calculate(Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task New_Property_Hiding__Non_Hidden_Virtual_Property_Can_Be_Configured()
    {
        // DerivedClientWithNewProperties hides Name but not Priority
        var mock = DerivedClientWithNewProperties.Mock();
        mock.Priority.Returns(42);

        var result = mock.Object.Priority;

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Mixed_New_And_Override__Override_Method_Can_Be_Configured()
    {
        var mock = MixedNewAndOverrideClient.Mock();
        mock.WithVersion(Any()).Returns("mocked-version");

        var result = mock.Object.WithVersion("v1");

        await Assert.That(result).IsEqualTo("mocked-version");
    }

    [Test]
    public async Task Mixed_New_And_Override__Override_Method_Falls_Back_To_Override()
    {
        var mock = MixedNewAndOverrideClient.Mock();

        // WithVersion is overridden (not hidden) — unconfigured should call the override
        var result = mock.Object.WithVersion("v1");

        await Assert.That(result).IsEqualTo("override-v1");
    }

    [Test]
    public async Task Mixed_New_And_Override__Non_Hidden_Virtual_Still_Works()
    {
        var mock = MixedNewAndOverrideClient.Mock();
        mock.Calculate(Any()).Returns(100);

        var result = mock.Object.Calculate(3);

        await Assert.That(result).IsEqualTo(100);
    }

    [Test]
    public async Task Mixed_New_And_Override__Override_Property_Can_Be_Configured()
    {
        var mock = MixedNewAndOverrideClient.Mock();
        mock.Priority.Returns(7);

        var result = mock.Object.Priority;

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    public async Task Three_Level_Hierarchy__Grandchild_Hides_Middle_Override()
    {
        // GrandchildClient hides MiddleClient.WithSnapshot (which overrides base)
        // Only Calculate should be mockable
        var mock = GrandchildClient.Mock();
        mock.Calculate(Any()).Returns(55);

        var result = mock.Object.Calculate(1);

        await Assert.That(result).IsEqualTo(55);
    }

    [Test]
    public async Task Three_Level_Hierarchy__Non_Hidden_Virtuals_Fall_Back_To_Base()
    {
        var mock = GrandchildClient.Mock();

        // WithVersion is not hidden at any level — unconfigured should call base
        var result = mock.Object.WithVersion("v2");

        await Assert.That(result).IsEqualTo("base-v2");
    }

    [Test]
    public async Task New_Virtual__Re_Introduced_Virtual_Can_Be_Configured()
    {
        // NewVirtualClient uses 'new virtual' — starts a new virtual chain
        var mock = NewVirtualClient.Mock();
        mock.WithSnapshot(Any()).Returns("mocked-snap");

        var result = mock.Object.WithSnapshot("s1");

        await Assert.That(result).IsEqualTo("mocked-snap");
    }

    [Test]
    public async Task New_Virtual__Unconfigured_Falls_Back_To_New_Virtual_Base()
    {
        var mock = NewVirtualClient.Mock();

        var result = mock.Object.WithSnapshot("s1");

        await Assert.That(result).IsEqualTo("newvirtual-s1");
    }

    [Test]
    public async Task New_Virtual_Derived__Can_Configure_Override_Of_New_Virtual()
    {
        var mock = NewVirtualDerivedClient.Mock();
        mock.WithSnapshot(Any()).Returns("fully-mocked");

        var result = mock.Object.WithSnapshot("s1");

        await Assert.That(result).IsEqualTo("fully-mocked");
    }

    [Test]
    public async Task New_Virtual_Derived__Unconfigured_Falls_Back_To_Derived_Override()
    {
        var mock = NewVirtualDerivedClient.Mock();

        var result = mock.Object.WithSnapshot("s1");

        await Assert.That(result).IsEqualTo("nvderived-s1");
    }

    [Test]
    public async Task Wrap_Mode__Class_With_New_Methods_Works()
    {
        var real = new WrapDerivedClientWithNew();
        var mock = Mock.Wrap(real);

        // Configure the non-hidden virtual
        mock.Compute(Any()).Returns(77);

        var result = mock.Object.Compute(5);

        await Assert.That(result).IsEqualTo(77);
    }

    [Test]
    public async Task Wrap_Mode__Class_With_New_Methods_Unconfigured_Delegates_To_Real()
    {
        var real = new WrapDerivedClientWithNew();
        var mock = Mock.Wrap(real);

        // Compute is virtual and unconfigured — should delegate to real instance (x + 1)
        var result = mock.Object.Compute(6);

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    public void Wrap_Mode__Verify_Calls_On_Class_With_New_Methods()
    {
        var real = new WrapDerivedClientWithNew();
        var mock = Mock.Wrap(real);

        mock.Object.Compute(10);
        mock.Object.Compute(20);

        mock.Compute(Any()).WasCalled(Times.Exactly(2));
    }

    // ========================================================================
    // Complex mixture test: many member types + overloads + new/override
    // ========================================================================

    [Test]
    public async Task Complex_Mixture__Non_Hidden_Overload_Can_Be_Configured()
    {
        // Execute(string,int,bool) is NOT hidden — should be mockable
        var mock = ComplexDerivedService.Mock();
        mock.Execute(Any(), Any(), Any()).Returns("mocked-3-arg");

        var result = mock.Object.Execute("cmd", 30, true);

        await Assert.That(result).IsEqualTo("mocked-3-arg");
    }

    [Test]
    public async Task Complex_Mixture__Non_Hidden_Overload_Falls_Back_To_Base()
    {
        var mock = ComplexDerivedService.Mock();

        // Execute(string,int,bool) not configured — should call base
        var result = mock.Object.Execute("cmd", 30, true);

        await Assert.That(result).IsEqualTo("base-exec-cmd-30-True");
    }

    [Test]
    public async Task Complex_Mixture__Override_Method_Can_Be_Configured()
    {
        var mock = ComplexDerivedService.Mock();
        mock.GetStatus().Returns(42);

        var result = mock.Object.GetStatus();

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Complex_Mixture__Override_Method_Falls_Back_To_Derived_Implementation()
    {
        var mock = ComplexDerivedService.Mock();

        // GetStatus is overridden — unconfigured should call the override (returns 1)
        var result = mock.Object.GetStatus();

        await Assert.That(result).IsEqualTo(1);
    }

    [Test]
    public async Task Complex_Mixture__Non_Hidden_Format_Overloads_Can_Be_Configured()
    {
        var mock = ComplexDerivedService.Mock();

        // Format(int) is NOT hidden
        mock.Format(Any<int>()).Returns("mocked-int-fmt");
        var result1 = mock.Object.Format(42);
        await Assert.That(result1).IsEqualTo("mocked-int-fmt");

        // Format(string,string) is NOT hidden
        mock.Format(Any(), Any()).Returns("mocked-locale-fmt");
        var result2 = mock.Object.Format("hello", "en-US");
        await Assert.That(result2).IsEqualTo("mocked-locale-fmt");
    }

    [Test]
    public async Task Complex_Mixture__Non_Hidden_Format_Overloads_Fall_Back_To_Base()
    {
        var mock = ComplexDerivedService.Mock();

        var result1 = mock.Object.Format(42);
        await Assert.That(result1).IsEqualTo("base-fmt-42");

        var result2 = mock.Object.Format("val", "fr-FR");
        await Assert.That(result2).IsEqualTo("base-fmt-val-fr-FR");
    }

    [Test]
    public async Task Complex_Mixture__Non_Hidden_Properties_Can_Be_Configured()
    {
        var mock = ComplexDerivedService.Mock();

        // Priority and Description are not hidden — should be mockable
        mock.Priority.Returns(10);
        mock.Description.Returns("mocked-desc");

        await Assert.That(mock.Object.Priority).IsEqualTo(10);
        await Assert.That(mock.Object.Description).IsEqualTo("mocked-desc");
    }

    [Test]
    public void Complex_Mixture__Verify_Calls_On_Non_Hidden_Members()
    {
        var mock = ComplexDerivedService.Mock();

        mock.Object.Execute("a", 1, true);
        mock.Object.Execute("b", 2, false);
        mock.Object.GetStatus();
        mock.Object.Format(99);

        mock.Execute(Any(), Any(), Any()).WasCalled(Times.Exactly(2));
        mock.GetStatus().WasCalled(Times.Once);
        mock.Format(Any<int>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Complex_Mixture__Non_Hidden_Event_Can_Be_Raised()
    {
        // StatusChanged is hidden by 'new'; MessageReceived remains virtual from base
        var mock = ComplexDerivedService.Mock();
        string? received = null;

        mock.Object.MessageReceived += (_, msg) => received = msg;
        mock.RaiseMessageReceived("hello");

        await Assert.That(received).IsEqualTo("hello");
    }
}
