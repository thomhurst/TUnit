namespace TUnit.TestProject;

public class TestDataSources
{
    public static int One() => 1;
    public static int Two() => 2;

    public static int[] OneEnumerable() => [1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
    public static int[] OneFailingEnumerable() => [1, 2, 3, 4, 5, 6, 7, 8, 9];

}