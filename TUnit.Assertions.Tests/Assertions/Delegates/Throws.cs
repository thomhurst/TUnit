using System.Runtime.CompilerServices;

namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    private static CustomException CreateCustomException(
        [CallerMemberName] string message = "",
        Exception? innerException = null)
    {
        return new CustomException(message, innerException);
    }

    private static SubCustomException CreateSubCustomException(
        [CallerMemberName] string message = "",
        Exception? innerException = null)
    {
        return new SubCustomException(message, innerException);
    }

    private static OtherException CreateOtherException(
        [CallerMemberName] string message = "",
        Exception? innerException = null)
    {
        return new OtherException(message, innerException);
    }

    private class CustomException : System.Exception
    {
        public CustomException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }

        public string Value => "Foo!";
    }

    private class SubCustomException : CustomException
    {
        public SubCustomException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }

    private class OtherException : System.Exception
    {
        public OtherException(string message, Exception? innerException = null)
            : base(message, innerException)
        {

        }
    }
}
