using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.PlatformSmoke;

public class SmokeTest
{
    [Test]
    public async Task RunsOnTheExpectedPlatform()
    {
        await Task.Yield();

#if BROWSER
        await Assert.That(OperatingSystem.IsBrowser()).IsTrue();
#elif ANDROID
        await Assert.That(OperatingSystem.IsAndroid()).IsTrue();
#elif IOS
        await Assert.That(OperatingSystem.IsIOS()).IsTrue();
#elif TVOS
        await Assert.That(OperatingSystem.IsTvOS()).IsTrue();
#elif MACCATALYST
        await Assert.That(OperatingSystem.IsMacCatalyst()).IsTrue();
#elif WASI
        await Assert.That(OperatingSystem.IsWasi()).IsTrue();
#endif
        await Assert.That(new List<int> { 1, 2 }.Count).IsEqualTo(2);
    }
}
