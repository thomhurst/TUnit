# Shared Instance Disposal Issue

## Problem
When using SharedType.PerClass, shared instances are not being disposed properly when tests are filtered. The reference counting approach doesn't work well because:

1. Shared instances are created lazily when first needed
2. Each test that runs tracks the shared instance (increments count)  
3. Each test's disposal event decrements the count
4. If only some tests run due to filtering, the count doesn't reach 0

## Root Cause
In `PropertyInjectionService.ProcessInjectedPropertyValue`, every property value is tracked with `ObjectTracker.TrackObject(events, propertyValue)`. For shared instances, this means the same object is tracked multiple times (once per test that uses it).

## Solution Approach
Need to distinguish between shared and non-shared instances:
- Non-shared (SharedType.None): Track per test (current behavior)
- Shared (SharedType.PerClass, etc.): Track only once when created, dispose when class completes

## Key Files
- TUnit.Core/PropertyInjectionService.cs - ProcessInjectedPropertyValue method
- TUnit.Core/TestDataContainer.cs - Manages shared instances
- TUnit.Core/Attributes/TestData/ClassDataSources.cs - Creates/retrieves shared instances
- TUnit.Core/Tracking/ObjectTracker.cs - Reference counting logic
- TUnit.Engine/Services/HookOrchestrator.cs - Executes After(Class) hooks