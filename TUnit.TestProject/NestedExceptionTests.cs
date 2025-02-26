namespace TUnit.TestProject;

public class NestedExceptionTests
{
    [Test]
    public void Test()
    {
        Method1();
    }

    private void Method1()
    {
        try
        {
            Method2();
        }
        catch (Exception e)
        {
            throw new Exception("Thrown from Method1", e);
        }
    }
    
    private void Method2()
    {
        try
        {
            Method3();
        }
        catch (Exception e)
        {
            throw new ArgumentException("Thrown from Method2", e);
        }
    }
    
    private void Method3()
    {
        throw new InvalidOperationException("Thrown from Method3");
    }
}