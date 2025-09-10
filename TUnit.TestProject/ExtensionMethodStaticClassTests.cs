namespace TUnit.TestProject;

// This is a static class with extension methods - should NOT be processed by PropertyInjectionSourceGenerator
public static class PhoneNumberFixtures
{
    public static PhoneNumber CreateValid(this PhoneNumber phoneNumber)
    {
        return new PhoneNumber("123-456-7890");
    }
    
    public static PhoneNumber CreateInvalid(this PhoneNumber phoneNumber) 
    {
        return new PhoneNumber("invalid");
    }
}

// Another static extension class 
public static class FirstNameFixtures  
{
    public static FirstName CreateValid(this FirstName firstName)
    {
        return new FirstName("John");
    }
}

public class PhoneNumber
{
    public PhoneNumber(string value)
    {
        Value = value;
    }
    
    public string Value { get; }
}

public class FirstName
{
    public FirstName(string value)
    {
        Value = value;
    }
    
    public string Value { get; }
}

// This is a regular class that SHOULD be processed if it has property injection attributes
public class RegularTestClass  
{
    // No property injection attributes, so should not generate anything
    public string? RegularProperty { get; set; }
}