namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Assembly)]
public class MaximumConcurrentTestsAttribute : Attribute
{
    public int MaximumConcurrentTests { get; }

    public MaximumConcurrentTestsAttribute(int maximumConcurrentTests)
    {
        if (maximumConcurrentTests < 1)
        {
            throw new ArgumentException("Maximum concurrency must be positive");
        }

        MaximumConcurrentTests = maximumConcurrentTests;
    }
}