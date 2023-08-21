using System;
using System.Runtime.InteropServices;

namespace TF.Tests.Unit;

public static class SystemInfo
{
    public static string GetOSPlatform()
    {
        string osPlatform = RuntimeInformation.OSDescription.ToLower();
        Architecture osArchitecture = RuntimeInformation.OSArchitecture;
        string platformPrefix = osPlatform switch
        {
            _ when osPlatform.Contains("darwin") => "darwin",
            _ when osPlatform.Contains("freebsd") => "freebsd",
            _ when osPlatform.Contains("linux") => "linux",
            _ when osPlatform.Contains("openbsd") => "openbsd",
            _ when osPlatform.Contains("solaris") => "solaris",
            _ when osPlatform.Contains("windows") => "windows",
            _ => "unknown"
        };

        return platformPrefix + "_" + GetArch(osArchitecture);
    }

    static string GetArch(Architecture architecture)
        => architecture switch
        {
            Architecture.X86 => "386",
            Architecture.X64 => "amd64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => "unknown"
        };
}
