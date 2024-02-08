namespace TUnit.Engine.Models;

internal record Filter
{
    public bool IsEmpty { get; private set; } = true;
    
    public List<string> RunnableCategories { get; } = new();
    public List<string> BannedCategories { get; } = new();
    public List<string> RunnableTestNames { get; } = new();
    public List<string> RunnableClasses { get; } = new();
    public List<string> RunnableFullyQualifiedClasses { get; } = new();

    public void AddFilter(string filterName, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        foreach (var value in rawValue.Split(','))
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            IsEmpty = false;

            if (string.Equals(filterName, TUnitTestProperties.TestName.Id,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                RunnableTestNames.Add(value);
            }

            if (string.Equals(filterName, nameof(TUnitTestProperties.TestClass),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                var collection = value.Contains('.') ? RunnableFullyQualifiedClasses : RunnableClasses;
                collection.Add(value);
            }

            if (string.Equals(filterName, TUnitTestProperties.Category.Id,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                RunnableCategories.Add(value);
            }
            
            if (string.Equals(filterName, TUnitTestProperties.NotCategory.Id,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                BannedCategories.Add(value);
            }
        }
    }
}