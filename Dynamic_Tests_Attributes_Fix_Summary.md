# Dynamic Tests Attributes Fix Summary

## Problem
Dynamic tests were not honoring attributes passed to them, particularly:
- `RepeatAttribute` - tests weren't being repeated
- `RetryAttribute` - retry logic wasn't applied
- `TimeoutAttribute` - timeouts weren't set
- `ExecutionPriorityAttribute` - priority wasn't being applied
- Any other attribute-based behaviors

## Root Cause
In `TestBuilderPipeline.cs`, dynamic tests were bypassing the normal test building process that handles attributes:
1. They created a single test instance regardless of `RepeatCount`
2. They didn't invoke discovery event receivers that handle attribute behaviors
3. They didn't populate TestDetails with attribute-derived values

## Solution Implemented

### 1. Added Repeat Support
- Added a loop to create multiple test instances based on `metadata.RepeatCount`
- Each repeated test gets a unique `RepeatIndex` in its TestData
- Display name shows repeat information when RepeatCount > 1

### 2. Added Event Receiver Support
- Added `EventReceiverOrchestrator` to `TestBuilderPipeline` constructor
- Added `InvokeDiscoveryEventReceiversAsync` method
- Now calls event receivers for each dynamic test to properly handle attributes like `ExecutionPriorityAttribute`

### 3. Populated TestDetails from Attributes
- Set `Timeout` from metadata.TimeoutMs
- Set `RetryLimit` from metadata.RetryCount
- Set `Attributes` from metadata.AttributeFactory
- Set `SkipReason` from metadata.SkipReason

### 4. Updated Dependency Injection
- Updated `TUnitServiceProvider` to pass `EventReceiverOrchestrator` to `TestBuilderPipeline`

## Code Changes

### TestBuilderPipeline.cs
```csharp
// Added repeat loop
for (var repeatIndex = 0; repeatIndex < Math.Max(1, metadata.RepeatCount); repeatIndex++)
{
    // Create test with unique RepeatIndex
    var testData = new TestBuilder.TestData { RepeatIndex = repeatIndex, ... };
    
    // Display repeat info in name
    var displayName = metadata.RepeatCount > 1 
        ? $"{metadata.TestName} (Repeat {repeatIndex + 1}/{metadata.RepeatCount})"
        : metadata.TestName;
    
    // Populate TestDetails from attributes
    testDetails.Timeout = metadata.TimeoutMs.HasValue 
        ? TimeSpan.FromMilliseconds(metadata.TimeoutMs.Value) 
        : null;
    testDetails.RetryLimit = metadata.RetryCount;
    
    // Invoke discovery event receivers
    await InvokeDiscoveryEventReceiversAsync(context);
}
```

## Result
- Dynamic tests now properly honor ALL attributes
- RepeatAttribute creates multiple test instances
- RetryAttribute sets retry limits
- TimeoutAttribute sets test timeouts
- ExecutionPriorityAttribute sets execution priority
- Any attribute implementing ITestDiscoveryEventReceiver is properly invoked
- Both TUnit.Core and TUnit.Engine build successfully without errors

## Key Insight
The main architectural issue was that dynamic tests were taking a shortcut that bypassed all the attribute processing logic. By ensuring they go through similar processing as regular tests (especially invoking discovery event receivers), all attribute-based behaviors now work correctly.