namespace TUnit.Profile;

public class Tests
{
    [Test]
    [Repeat(50)]
    public void Basic()
    {
        _ = 1 + 1;
    }

    [Test]
    [Arguments(1, 2)]
    [Arguments(3, 4)]
    [Repeat(50)]
    public void Basic(int a, int b)
    {
        _ = a + b;
    }
}
