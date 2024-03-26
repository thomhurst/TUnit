using TUnit.Core;

namespace TUnit.TestProject;

[Retry(3)]
public class RetryTests
{
    [Test]
    [Retry(1)]
    public void One()
    {
    }
    
    [Test]
    [Retry(2)]
    public void Two()
    {
    }
    
    [Test]
    public void Three()
    {
    }
}