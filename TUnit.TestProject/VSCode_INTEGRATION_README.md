# VSCode Test Discovery Validation

This directory contains test files to validate that VSCode test discovery is working correctly with TUnit.

## How to Test VSCode Integration

1. **Prerequisites:**
   - Install [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
   - Enable "Use Testing Platform Protocol" in VSCode settings
   - Ensure you have TUnit 0.5x or later

2. **Expected Behavior:**
   - Open `VSCodeIntegrationTest.cs` in VSCode
   - You should see "play" button icons (▶️) in the editor gutter next to each test method
   - Clicking these buttons should run the individual tests
   - The Test Explorer should also show all tests

3. **Troubleshooting:**
   - If you don't see play buttons, check that "Use Testing Platform Protocol" is enabled
   - Restart VSCode after enabling the setting
   - Check that the project builds successfully with `dotnet build`
   - Verify that your project references TUnit correctly

## Properties Added for VSCode Integration

The following MSBuild properties have been added to enable VSCode test discovery:

```xml
<UseTestingPlatformProtocol>true</UseTestingPlatformProtocol>
<EnableMSTestRunner>true</EnableMSTestRunner>
<IsTestingPlatformEnabled>true</IsTestingPlatformEnabled>
```

These properties are automatically included when you use TUnit 0.5x or later.