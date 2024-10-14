using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class StringEqualsAssertionTests
{
    [Test]
    public async Task Equals_Success()
    {
        var value1 = "Foo";
        var value2 = "Foo";
        await TUnitAssert.That(value1).IsEqualTo(value2);
    }
    
    [Test]
    public async Task Equals_Trimmed1_Success()
    {
        var value1 = "Foo";
        var value2 = "Foo ";
        await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming();
    }
    
    [Test]
    public async Task Equals_Trimmed2_Success()
    {
        var value1 = "Foo ";
        var value2 = "Foo";
        await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming();
    }
    
    [Test]
    public async Task IgnoringWhitespace_Success()
    {
        var value1 = "       F    o    o    ";
        var value2 = "Foo";
        await TUnitAssert.That(value1).IsEqualTo(value2).IgnoringWhitespace();
    }
    
    [Test]
    public async Task Equals_NullAndEmptyEquality_Success()
    {
        var value1 = "";
        string? value2 = null;
        await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality();
    }
    
    [Test]
    public async Task Equals_NullAndEmptyEquality2_Success()
    {
        string? value1 = null;
        var value2 = "";
        
        await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality();
    }
    
    [Test]
    public void Equals_Failure()
    {
        var value1 = "Foo";
        var value2 = "Bar";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
    [Test]
    public void Equals_Trimmed1_Failure()
    {
        var value1 = "Foo";
        var value2 = "Foo! ";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming());
    }
    
    [Test]
    public void Equals_Trimmed2_Failure()
    {
        var value1 = "Foo! ";
        var value2 = "Foo";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithTrimming());
    }
    
    [Test]
    public void IgnoringWhitespace_Failure()
    {
        var value1 = "       F    o    o    !";
        var value2 = "Foo";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).IgnoringWhitespace());
    }
    
    [Test]
    public void Equals_NullAndEmptyEquality_Failure()
    {
        var value1 = "1";
        string? value2 = null;
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality());
    }
    
    [Test]
    public void Equals_NullAndEmptyEquality2_Failure()
    {
        string? value1 = null;
        var value2 = "1";
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithNullAndEmptyEquality());
    }
    
    [Test]
    public void Equals_Failure_Long_Message()
    {
        var value1 = """
                     Lorem ipsum dolor sit amet diam duo amet sea rebum. 
                     Et voluptua ex voluptua no praesent diam eu sed consetetur sit at ipsum et consetetur aliquam ipsum dolor. 
                     Et et sit nulla hendrerit ipsum stet ut quod rebum. 
                     Facer kasd et dolor ea justo. 
                     Nulla qui ut takimata sadipscing sanctus magna et aliquip sed lorem rebum rebum nonumy dolores kasd ipsum ipsum sea. 
                     Iriure et augue feugiat eirmod et et rebum placerat gubergren nulla voluptua illum.
                     Rebum sea aliquyam illum eos dolores duo justo dolor sit lorem et eu stet amet amet.
                     Consequat odio ea veniam.
                     Amet enim in gubergren stet rebum consetetur nonumy eirmod elitr.
                     Dolores ex clita voluptua et magna dolor justo clita dolor erat sed erat.
                     Cum sadipscing sit tempor dolore elitr.
                     Ullamcorper ipsum erat labore esse diam et tation magna elitr lorem est eirmod lorem ad dignissim ipsum. 
                     Et duo et elit.
                     Aliquyam dolores sed elitr sit diam sed stet diam diam. 
                     Erat ea vero blandit elitr sea hendrerit aliquyam sanctus lobortis ipsum clita. 
                     Eu magna dolores justo kasd aliquyam augue et sed ipsum et stet dolores aliquyam et eos erat diam duo. 
                     Quis duo feugait erat diam. Amet minim vero veniam esse consequat tation takimata eu in diam ut ea hendrerit eos gubergren ea eirmod. 
                     Volutpat vero est ea clita clita magna dolor nulla ipsum aliquyam nonumy.
                     """.ReplaceLineEndings(" ");
        
        var value2 = """
                     Lorem ipsum dolor sit amet diam duo amet sea rebum. 
                     Et voluptua ex voluptua no praesent diam eu sed consetetur sit at ipsum et consetetur aliquam ipsum dolor. 
                     Et et sit nulla hendrerit ipsum stet ut quod rebum. 
                     Facer kasd et dolor ea justo. 
                     Nulla qui ut takimata sadipscing sanctus magna et aliquip sed lorem rebum rebum nonumy dolores kasd ipsum ipsum sea. 
                     Iriure et augue feugiat eirmod et et rebum placerat gubergren nulla voluptua illum.
                     Rebum sea aliquyam illum eos dolores duo justo dolor sit lorem et eu stet amet amet.
                     Consequat odio ea veniam!
                     Amet enim in gubergren stet rebum consetetur nonumy eirmod elitr.
                     Dolores ex clita voluptua et magna dolor justo clita dolor erat sed erat.
                     Cum sadipscing sit tempor dolore elitr.
                     Ullamcorper ipsum erat labore esse diam et tation magna elitr lorem est eirmod lorem ad dignissim ipsum. 
                     Et duo et elit.
                     Aliquyam dolores sed elitr sit diam sed stet diam diam. 
                     Erat ea vero blandit elitr sea hendrerit aliquyam sanctus lobortis ipsum clita. 
                     Eu magna dolores justo kasd aliquyam augue et sed ipsum et stet dolores aliquyam et eos erat diam duo. 
                     Quis duo feugait erat diam. Amet minim vero veniam esse consequat tation takimata eu in diam ut ea hendrerit eos gubergren ea eirmod. 
                     Volutpat vero est ea clita clita magna dolor nulla ipsum aliquyam nonumy.
                     """.ReplaceLineEndings(" ");
        
        var exception = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
        NUnitAssert.That(exception!.Message, Is.EqualTo("""
                                                        Expected value1 to be equal to "Lorem ipsum dolor sit amet diam duo amet sea rebum.  Et voluptua ex voluptua no praesent diam eu se…
                                                        
                                                        but found "Lorem ipsum dolor sit amet diam duo amet sea rebum.  Et voluptua ex voluptua no praesent diam eu se… which differs at index 556:
                                                                                    ↓
                                                           "Consequat odio ea veniam. Amet enim in gubergren s…"
                                                           "Consequat odio ea veniam! Amet enim in gubergren s…"
                                                                                    ↑
                                                        
                                                        at Assert.That(value1).IsEqualTo(value2)
                                                        """));
    }
}