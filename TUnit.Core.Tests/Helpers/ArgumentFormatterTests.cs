using TUnit.Core.Helpers;

namespace TUnit.Core.Tests.Helpers;

public class ArgumentFormatterTests
{
    [Test]
    public void FormatDefault_ValueTuple_FormatsCorrectly()
    {
        var tuple = (1, "test", true);
        
        var result = ArgumentFormatter.Format(tuple, []);
        
        Assert.That(result, Is.EqualTo("(1, test, true)"));
    }
    
    [Test]
    public void FormatDefault_NestedValueTuple_FormatsCorrectly()
    {
        var tuple = (1, (2, "inner"), true);
        
        var result = ArgumentFormatter.Format(tuple, []);
        
        Assert.That(result, Is.EqualTo("(1, (2, inner), true)"));
    }
    
    [Test]
    public void FormatDefault_SystemTuple_FormatsCorrectly()
    {
        var tuple = Tuple.Create(1, "test", false);
        
        var result = ArgumentFormatter.Format(tuple, []);
        
        Assert.That(result, Is.EqualTo("(1, test, false)"));
    }
    
    [Test]
    public void FormatArguments_WithTuples_FormatsCorrectly()
    {
        var args = new object?[] { (1, "a"), 42, (true, 3.14) };
        
        var result = ArgumentFormatter.FormatArguments(args);
        
        Assert.That(result, Is.EqualTo("(1, a), 42, (true, 3.14)"));
    }
    
    [Test]
    public void FormatDefault_LargeTuple_FormatsAllElements()
    {
        var tuple = (1, 2, 3, 4, 5, 6, 7, 8);
        
        var result = ArgumentFormatter.Format(tuple, []);
        
        Assert.That(result, Is.EqualTo("(1, 2, 3, 4, 5, 6, 7, 8)"));
    }
    
    [Test]
    public void FormatDefault_TupleWithNull_HandlesNullCorrectly()
    {
        var tuple = (1, null, "test");
        
        var result = ArgumentFormatter.Format(tuple, []);
        
        Assert.That(result, Is.EqualTo("(1, null, test)"));
    }
}