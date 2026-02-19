using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;

namespace TUnit.Core;

public partial class TestContext
{
    internal readonly List<TestDetails> _dependencies = [];
    internal bool _dependenciesPopulated;
    internal string? ParentTestId { get; set; }
    internal TestRelationship Relationship { get; set; } = TestRelationship.None;

    IReadOnlyList<TestDetails> ITestDependencies.DependsOn => _dependencies;
    string? ITestDependencies.ParentTestId => ParentTestId;
    TestRelationship ITestDependencies.Relationship => Relationship;

    IReadOnlyList<TestContext> ITestDependencies.GetTests(Func<TestContext, bool> predicate) => GetTests(predicate);
    IReadOnlyList<TestContext> ITestDependencies.GetTests(string testName) => GetTests(testName);
    IReadOnlyList<TestContext> ITestDependencies.GetTests(string testName, Type classType) => GetTests(testName, classType);

    internal List<TestContext> GetTests(Func<TestContext, bool> predicate)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        var classType = TestDetails?.ClassType;
        if (classType == null)
        {
            return [];
        }

        var allTests = testFinder.GetTests(classType);
        var tests = new List<TestContext>();
        var hasUnfinished = false;

        foreach (var test in allTests)
        {
            if (predicate(test))
            {
                tests.Add(test);
                if (test.Result == null)
                {
                    hasUnfinished = true;
                }
            }
        }

        if (hasUnfinished)
        {
            throw new InvalidOperationException(
                "Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?"
            );
        }

        return tests;
    }

    internal List<TestContext> GetTests(string testName)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        var classType = TestDetails.ClassType;

        var testsArray = testFinder.GetTestsByNameAndParameters(
            testName,
            [],
            classType,
            [],
            []
        );

        foreach (var test in testsArray)
        {
            if (test.Result == null)
            {
                throw new InvalidOperationException(
                    "Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?"
                );
            }
        }

        return new List<TestContext>(testsArray);
    }

    internal List<TestContext> GetTests(string testName, Type classType)
    {
        var testFinder = ServiceProvider.GetService<ITestFinder>()!;

        var testsArray = testFinder.GetTestsByNameAndParameters(
            testName,
            [],
            classType,
            [],
            []
        );

        foreach (var test in testsArray)
        {
            if (test.Result == null)
            {
                throw new InvalidOperationException(
                    "Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?"
                );
            }
        }

        return new List<TestContext>(testsArray);
    }
}
