using TUnit.Core.Exceptions;
using TUnit.Engine.Building;

namespace TUnit.UnitTests;

/// <summary>
/// The engine unpacks a constraint-violating row inside the row's catch block, so unpacking must not
/// throw, or the method's remaining rows are lost (#7123).
/// </summary>
public class ConstraintViolationUnpackingTests
{
    [Test]
    public async Task Reports_Plain_GenericTypeResolutionException_With_Unwrapped_Row()
    {
        var value = new object();
        var exception = new GenericConstraintViolationRowException("violation", [new TestDataRow<object>(value)]);

        var (reported, rowData) = TestBuilder.UnpackConstraintViolation(exception, []);

        await Assert.That(reported.GetType()).IsEqualTo(typeof(GenericTypeResolutionException));
        await Assert.That(reported.Message).IsEqualTo("violation");
        await Assert.That(rowData).IsNotNull();
        await Assert.That(rowData![0]).IsSameReferenceAs(value);
    }

    [Test]
    public async Task Throwing_Row_Func_Tracks_Nothing()
    {
        // The Func threw, so it created no value. The TestDataRow wrapper itself is not worth tracking.
        object?[] row = [new TestDataRow<Func<object>>(() => throw new InvalidOperationException("factory"))];
        var exception = new GenericConstraintViolationRowException("violation", row);

        var (reported, rowData) = TestBuilder.UnpackConstraintViolation(exception, []);

        await Assert.That(reported.GetType()).IsEqualTo(typeof(GenericTypeResolutionException));
        await Assert.That(reported.Message).IsEqualTo("violation");
        await Assert.That(rowData).IsNotNull();
        await Assert.That(rowData!).IsEmpty();
    }

    [Test]
    public async Task Other_Exceptions_Pass_Through()
    {
        var exception = new GenericTypeResolutionException("other");

        var (reported, rowData) = TestBuilder.UnpackConstraintViolation(exception, []);

        await Assert.That(reported).IsSameReferenceAs(exception);
        await Assert.That(rowData).IsNull();
    }

    [Test]
    public async Task Failed_Test_Factories_Never_Report_The_Internal_Row_Exception()
    {
        // Every failed-test factory passes its exception through this, including catch sites that do not unpack.
        var reported = TestBuilder.ToReportedException(new GenericConstraintViolationRowException("violation", null));

        await Assert.That(reported.GetType()).IsEqualTo(typeof(GenericTypeResolutionException));
        await Assert.That(reported.Message).IsEqualTo("violation");

        var other = new InvalidOperationException("other");
        await Assert.That(TestBuilder.ToReportedException(other)).IsSameReferenceAs(other);
    }
}
