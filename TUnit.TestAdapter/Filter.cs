using TUnit.TestAdapter.Constants;

namespace TUnit.TestAdapter;

public record Filter
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
            
            switch (filterName)
            {
                case TestAdapterConstants.Filters.TestName:
                    RunnableTestNames.Add(value);
                    continue;
                case TestAdapterConstants.Filters.TestClass:
                    if (value.Contains('.'))
                    {
                        RunnableFullyQualifiedClasses.Add(value);
                    }
                    else
                    {
                        RunnableClasses.Add(value);
                    }

                    continue;
                case TestAdapterConstants.Filters.Category:
                    RunnableCategories.Add(value);
                    continue;
            }
        }
    }
}