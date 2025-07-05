namespace TUnit.Engine.Helpers;

public static class DotNetAssemblyHelper
{
    private static readonly byte[] _mscorlibPublicKeyToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
    private static readonly byte[] _netFrameworkPublicKeyToken = [0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a];
    private static readonly byte[] _netCorePublicKeyToken = [0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51];
    private static readonly byte[] _systemPrivatePublicKeyToken = [0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e];

    public static bool IsInDotNetCoreLibrary(Type type)
    {
        return IsDotNetCoreLibrary(type.Assembly.GetName().GetPublicKeyToken());
    }

    public static bool IsDotNetCoreLibrary(byte[]? publicKeyToken)
    {
        if (publicKeyToken is null)
        {
            return false;
        }

        return publicKeyToken.SequenceEqual(_mscorlibPublicKeyToken)
            || publicKeyToken.SequenceEqual(_netFrameworkPublicKeyToken)
            || publicKeyToken.SequenceEqual(_netCorePublicKeyToken)
            || publicKeyToken.SequenceEqual(_systemPrivatePublicKeyToken);
    }
}
