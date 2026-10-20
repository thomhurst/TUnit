using TUnit.Engine.Extensions;

namespace TUnit.UnitTests;

/// <summary>
/// The class name reported for a test (TRX className and the JSON model's ClassType) is the generic
/// type definition's full name, so reflection mode's closed types report the same name as
/// source-generated mode's open ones (#7151).
/// </summary>
public class ReportedClassFullNameTests
{
    [Test]
    public async Task Closed_Generic_Type_Reports_Its_Generic_Type_Definition()
    {
        await Assert.That(TestExtensions.GetReportedClassFullName(typeof(Holder<int>)))
            .IsEqualTo("TUnit.UnitTests.ReportedClassFullNameTests+Holder`1");
    }

    [Test]
    public async Task Open_Generic_Type_Reports_Its_Full_Name()
    {
        await Assert.That(TestExtensions.GetReportedClassFullName(typeof(Holder<>)))
            .IsEqualTo("TUnit.UnitTests.ReportedClassFullNameTests+Holder`1");
    }

    [Test]
    public async Task Non_Generic_Type_Reports_Its_Full_Name()
    {
        await Assert.That(TestExtensions.GetReportedClassFullName(typeof(ReportedClassFullNameTests)))
            .IsEqualTo("TUnit.UnitTests.ReportedClassFullNameTests");
    }

    private sealed class Holder<T>;
}
