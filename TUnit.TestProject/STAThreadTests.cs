using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Executors;

namespace TUnit.TestProject;

public class STAThreadTests
{
    [Before(EachTest)]
    [HookExecutor<STAThreadExecutor>]
    public void BeforeTest()
    {
    }
    
    [After(EachTest)]
    [HookExecutor<STAThreadExecutor>]
    public void AfterTest()
    {
    }
    
    [Test, TestExecutor<STAThreadExecutor>]
    public async Task With_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).Is.EqualTo(ApartmentState.STA);
    }
    
    [Test]
    public async Task Without_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).Is.EqualTo(ApartmentState.MTA);
    }
}