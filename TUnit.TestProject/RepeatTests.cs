using TUnit.Core;

namespace TUnit.TestProject;

[Repeat(3)]
public class RepeatTests
{
    [Test]
    [Repeat(1)]
    public void One()
    {
    }
    
    [Test]
    [Repeat(2)]
    public void Two()
    {
    }
    
    [Test]
    public void Three()
    {
    }
}