using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter;

public record Filter
{
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
            switch (filterName)
            {
                case TestAdapterConstants.Filters.TestName:
                    RunnableTestNames.Add(value);
                    return;
                case TestAdapterConstants.Filters.TestClasses:
                    if (value.Contains('.'))
                    {
                        RunnableFullyQualifiedClasses.Add(value);
                    }
                    else
                    {
                        RunnableClasses.Add(value);
                    }

                    return;
                case TestAdapterConstants.Filters.Categories:
                    RunnableCategories.Add(value);
                    return;
            }
        }
    }
}