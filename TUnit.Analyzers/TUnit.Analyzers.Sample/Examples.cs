// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.Analyzers.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    public class CommonClass // Try to apply quick fix using the IDE.
    {
    }
    
    // [DataDrivenTest]
    // public void No_Arg()
    // {
    // }
    
    // [DataDrivenTest()]
    // public void No_Arg2()
    // {
    // }
    
    // [DataDrivenTest("")]
    // public void WrongType(int i)
    // {
    // }
    
        
    // [DataDrivenTest("")]
    // public void WrongType()
    // {
    // }
    
    [DataDrivenTest("")]
    public void WrongType(string value)
    {
    }

    public async Task ToStars()
    {
        await Assert.That("1").Is.EqualTo("2");
        var spaceship = new Spaceship();
        spaceship.SetSpeed(300000000); // Invalid value, it should be highlighted.
        spaceship.SetSpeed(42);
    }
}