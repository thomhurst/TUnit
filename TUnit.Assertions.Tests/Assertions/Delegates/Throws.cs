using System.Runtime.CompilerServices;

namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    private class CustomException : System.Exception
    {
        public CustomException(string message) : base(message)
        {

        }

        public static CustomException Create([CallerMemberName] string message = "")
        {
            return new CustomException(message);
        }
    }

    private class SubCustomException : CustomException
    {
        public SubCustomException(string message) : base(message)
        {

        }

        public static SubCustomException Create([CallerMemberName] string message = "")
        {
            return new SubCustomException(message);
        }
    }

    private class OtherException : System.Exception
    {
        public OtherException(string message) : base(message)
        {

        }

        public static OtherException Create([CallerMemberName] string message = "")
        {
            return new OtherException(message);
        }
    }

}
