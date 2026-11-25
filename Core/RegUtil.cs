using Microsoft.Win32;

namespace TrapShack.Core;

public static class RegUtil
{
    /// <summary>Always the 64-bit registry view, so a 32-bit process never gets redirected.</summary>
    public static RegistryKey Base(RegistryHive hive) => RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);

    public static object? Read(RegistryHive hive, string path, string name)
    {
        try
        {
            using var b = Base(hive);
            using var k = b.OpenSubKey(path);
            return k?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        }
        catch { return null; }
    }

    public static string? ReadString(RegistryHive hive, string path, string name) => Read(hive, path, name)?.ToString();

    public static int? ReadInt(RegistryHive hive, string path, string name) => Read(hive, path, name) is int i ? i : null;

    public static bool KeyExists(RegistryHive hive, string path)
    {
        try
        {
            using var b = Base(hive);
            using var k = b.OpenSubKey(path);
            return k != null;
        }
        catch { return false; }
    }
}
