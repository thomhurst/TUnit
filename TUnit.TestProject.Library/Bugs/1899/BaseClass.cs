namespace TUnit.TestProject.Library.Bugs._1899;

public abstract class BaseClass<T>
{
    private int _value;
    
    [Before(HookType.Test)]
    public void Setup()
    {
        _value = 99;
    }

    [Test]
    public void Test1()
    {
        if (_value != 99)
        {
            throw new Exception("Setup method was not called");
        }
    }
}