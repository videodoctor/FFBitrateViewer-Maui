using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace FFBitrateViewer.ApplicationAvalonia.Services;

public abstract class OSPlatformClient
{
    public static OSPlatformClient GetOSPlatformClient()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsPlatformClient();
        }
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //{
        //    yield return new ShCommandComposer();
        //}
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //{
        //    yield return new ZshCommandComposer();
        //}
        else
        {
            throw new OSPlatformClientException($"Unsupported OS: {RuntimeInformation.OSDescription}");
        }
    }

    public abstract bool IsDark();

    public abstract bool IsLight();
}

public class OSPlatformClientException : FFBitrateViewerException
{
    public OSPlatformClientException() { }

    public OSPlatformClientException(string message) : base(message) { }

    public OSPlatformClientException(string message, Exception inner) : base(message, inner) { }

}

public class WindowsPlatformClient : OSPlatformClient
{
    private const int HKEY_CURRENT_USER = unchecked((int)0x80000001);

    private const int KEY_READ = 0x00020019;

    private const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

    [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
    private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int ulOptions, int samDesired, out IntPtr hkResult);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
    private static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, IntPtr lpReserved, out uint lpType, out int lpData, ref uint lpcbData);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

    private static string Theme()
    {
        try
        {
            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (registryKey is not null && registryKey.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0 ? "Dark" : "Light";
            }
        }
        catch (Exception exception)
        {
            throw new OSPlatformClientException($"Failed on {RuntimeInformation.OSDescription} while trying to retrieve default theme.", exception);
        }

        return string.Empty;
    }

    public override bool IsDark()
        => string.Equals(Theme(), "Dark", StringComparison.OrdinalIgnoreCase);

    public override bool IsLight()
        => string.Equals(Theme(), "Light", StringComparison.OrdinalIgnoreCase);

    private static void Listener(Action<string> callback)
    {
        IntPtr hKey;
        RegOpenKeyEx((IntPtr)HKEY_CURRENT_USER, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", 0, KEY_READ, out hKey);

        uint dwSize = sizeof(int);
        int queryValueLast = 0;
        int queryValue = 0;
        uint lpType;

        RegQueryValueEx(hKey, "AppsUseLightTheme", IntPtr.Zero, out lpType, out queryValueLast, ref dwSize);

        while (true)
        {
            RegNotifyChangeKeyValue(hKey, true, REG_NOTIFY_CHANGE_LAST_SET, IntPtr.Zero, false);
            RegQueryValueEx(hKey, "AppsUseLightTheme", IntPtr.Zero, out lpType, out queryValue, ref dwSize);

            if (queryValueLast != queryValue)
            {
                queryValueLast = queryValue;
                callback(queryValue != 0 ? "Light" : "Dark");
            }
        }
    }
}