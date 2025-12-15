namespace TUnit.Assertions.Tests.AssertConditions;

public class BecauseOutputFormatTest
{
    [Test]
    public async Task Because_Message_Appears_On_Expected_Line()
    {
        // This test verifies the exact format requested in the issue
        var expectedMessage = """
                              Expected to be equal to "alpha2", because groups should be self-contained
                              but found "alpha"

                              at Assert.That(config).IsEqualTo("alpha2").Because("groups should be self-contained")
                              """;
        
        var config = "alpha";

        var action = async () =>
        {
            await Assert.That(config).IsEqualTo("alpha2").Because("groups should be self-contained");
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage.NormalizeLineEndings());
    }

    [Test]
    public async Task Because_Message_With_And_Assertion()
    {
        // Verify that And assertions also show because messages inline
        var expectedMessage = """
                              Expected to be true, because first condition must hold
                              and to be false, because second condition must also hold
                              but found True

                              at Assert.That(variable).IsTrue().Because("first condition must hold").And.IsFalse().Because("second condition must also hold")
                              """;
        
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsTrue().Because("first condition must hold")
                .And.IsFalse().Because("second condition must also hold");
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage.NormalizeLineEndings());
    }

    [Test]
    public async Task Because_Message_With_Or_Assertion()
    {
        // Verify that Or assertions also show because messages inline
        var expectedMessage = """
                              Expected to be false, because first condition should pass
                              or to be null, because second condition should pass
                              but found True

                              at Assert.That(variable).IsFalse().Because("first condition should pass").Or.IsNull().Because("second condition should pass")
                              """;
        
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse().Because("first condition should pass")
                .Or.IsNull().Because("second condition should pass");
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage.NormalizeLineEndings());
    }
}
