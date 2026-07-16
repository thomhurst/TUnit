namespace TUnit.TestProject;

using TUnit.Core.Interfaces;

public class DynamicSkipReasonTests
{
    [Test]
    [CustomSkipViaSetSkipped("TestDevice123")]
    public void TestSkippedViaSetSkippedMethod()
    {
        throw new Exception("This test should have been skipped!");
    }

    [Test]
    [CustomSkipViaGetSkipReason("CustomDevice456")]
    public void TestSkippedViaGetSkipReasonOverride()
    {
        throw new Exception("This test should have been skipped!");
    }

    [Test]
    [ConditionalSkipAttribute("AllowedDevice")]
    public void TestNotSkippedWhenConditionFalse()
    {
    }
}

/// <summary>
/// Custom attribute that uses TestRegisteredContext.SetSkipped() to skip tests dynamically
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CustomSkipViaSetSkippedAttribute : Attribute, ITestRegisteredEventReceiver
{
    private readonly string _deviceName;

    public CustomSkipViaSetSkippedAttribute(string deviceName)
    {
        _deviceName = deviceName;
    }

    public int Order => int.MinValue;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetSkipped($"Test '{context.TestName}' is not supported on device '{_deviceName}'");
        return default;
    }
}

/// <summary>
/// Custom SkipAttribute that overrides GetSkipReason() to provide dynamic skip reasons
/// </summary>
public class CustomSkipViaGetSkipReasonAttribute : SkipAttribute
{
    private readonly string _deviceName;

    public CustomSkipViaGetSkipReasonAttribute(string deviceName)
        : base("Device-specific skip")
    {
        _deviceName = deviceName;
    }

    protected override string GetSkipReason(TestRegisteredContext context)
    {
        return $"Test '{context.TestName}' skipped for device '{_deviceName}' via GetSkipReason override";
    }
}

/// <summary>
/// Conditional skip attribute that only skips if device name is not in allowed list
/// </summary>
public class ConditionalSkipAttribute : SkipAttribute
{
    private readonly string _deviceName;
    private static readonly string[] AllowedDevices = ["AllowedDevice", "AnotherAllowedDevice"];

    public ConditionalSkipAttribute(string deviceName)
        : base("Device not allowed")
    {
        _deviceName = deviceName;
    }

    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        bool shouldSkip = !AllowedDevices.Contains(_deviceName);
        return Task.FromResult(shouldSkip);
    }

    protected override string GetSkipReason(TestRegisteredContext context)
    {
        return $"Test '{context.TestName}' skipped because device '{_deviceName}' is not in allowed list";
    }
}
