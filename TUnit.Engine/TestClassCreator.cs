using TUnit.Core;

namespace TUnit.Engine;

public class TestClassCreator
{
    public object CreateTestClass(TestDetails testDetails)
    {
        try
        {
            return Activator.CreateInstance(testDetails.MethodInfo.DeclaringType!)!;
        }
        catch (Exception e)
        {
            throw new Exception("Cannot create an instance of the test class. Is there a public parameterless constructor?", e);
        }
    }   
}