using TUnit.Core.Helpers;

namespace TUnit.Core.Tests.Helpers;

public class ArgumentFormatterTests
{
    [Test]
    public void FormatDefault_ValueTuple_FormatsCorrectly()
    {
        // Arrange
        var tuple = (1, "test", true);
        
        // Act
        var result = ArgumentFormatter.Format(tuple, []);
        
        // Assert
        Assert.That(result, Is.EqualTo("(1, test, true)"));
    }
    
    [Test]
    public void FormatDefault_NestedValueTuple_FormatsCorrectly()
    {
        // Arrange
        var tuple = (1, (2, "inner"), true);
        
        // Act
        var result = ArgumentFormatter.Format(tuple, []);
        
        // Assert
        Assert.That(result, Is.EqualTo("(1, (2, inner), true)"));
    }
    
    [Test]
    public void FormatDefault_SystemTuple_FormatsCorrectly()
    {
        // Arrange
        var tuple = Tuple.Create(1, "test", false);
        
        // Act
        var result = ArgumentFormatter.Format(tuple, []);
        
        // Assert
        Assert.That(result, Is.EqualTo("(1, test, false)"));
    }
    
    [Test]
    public void FormatArguments_WithTuples_FormatsCorrectly()
    {
        // Arrange
        var args = new object?[] { (1, "a"), 42, (true, 3.14) };
        
        // Act
        var result = ArgumentFormatter.FormatArguments(args);
        
        // Assert
        Assert.That(result, Is.EqualTo("(1, a), 42, (true, 3.14)"));
    }
    
    [Test]
    public void FormatDefault_LargeTuple_FormatsAllElements()
    {
        // Arrange
        var tuple = (1, 2, 3, 4, 5, 6, 7, 8);
        
        // Act
        var result = ArgumentFormatter.Format(tuple, []);
        
        // Assert
        Assert.That(result, Is.EqualTo("(1, 2, 3, 4, 5, 6, 7, 8)"));
    }
    
    [Test]
    public void FormatDefault_TupleWithNull_HandlesNullCorrectly()
    {
        // Arrange
        var tuple = (1, null, "test");
        
        // Act
        var result = ArgumentFormatter.Format(tuple, []);
        
        // Assert
        Assert.That(result, Is.EqualTo("(1, null, test)"));
    }
}