using System;
using TUnit.Assertions.Attributes;

namespace TestDemo;

[CreateAssertion(typeof(char), nameof(char.IsDigit), AssertionType.Is | AssertionType.IsNot)]
[CreateAssertion(typeof(char), nameof(char.IsLetter), AssertionType.Is)]
public static partial class CharAssertions
{
    // Generated methods will appear here
}

// Usage example:
// public class MyTests
// {
//     [Test]
//     public void TestCharacterValidation()
//     {
//         await Assert.That('5').IsDigit();
//         await Assert.That('a').IsNotDigit();
//         await Assert.That('A').IsLetter();
//     }
// }