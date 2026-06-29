using Microsoft.Win32;

namespace OpenSnap;

/// <summary>
/// Manages the "Launch at startup" toggle via the Windows
/// current-user Registry Run key.
/// </summary>
public static class StartupManager
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string EntryName = "OpenSnap";

    /// <summary>Returns the full path to the running executable.</summary>
    private static string ExecutablePath =>
        Environment.ProcessPath ?? string.Empty;

    /// <summary>Check whether the startup entry exists and points to this exe.</summary>
    public static bool IsRegisteredForStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            if (key?.GetValue(EntryName) is string current)
                return current.Equals(ExecutablePath, StringComparison.OrdinalIgnoreCase);
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Register (or remove) the startup entry.</summary>
    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
            if (key is null) return;

            if (enable)
                key.SetValue(EntryName, ExecutablePath);
            else
                key.DeleteValue(EntryName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update startup: {ex.Message}");
        }
    }
}
