using System.Diagnostics;
using System.Security.Principal;

namespace SecureTPMVault;

/// <summary>
/// Provides helper methods for managing Windows User Privileges.
/// </summary>
internal static class AdminHelper
{
    /// <summary>
    /// Checks if the current process is running with Administrator privileges.
    /// </summary>
    /// <returns>True if the current user is an Administrator, otherwise false.</returns>
    public static bool IsRunningAsAdmin()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Restarts the current application with the "runas" verb to trigger UAC and gain Administrator privileges.
    /// The current process terminates after launching the new one.
    /// </summary>
    /// <param name="args">Command line arguments to pass to the restarted process. Defaults to an empty array.</param>
    public static void RestartAsAdmin(string[]? args = null)
    {
        args ??= [];

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = string.Join(" ", args)
            });
        }
        catch (Exception ex)
        {
            // Since we are restarting, we can't use the NotificationService UI queue effectively here.
            // Console output is the safest fallback.
            Console.ForegroundColor = Theme.ErrorColor;
            Console.WriteLine($"[ERROR] Failed to restart as admin: {ex.Message}");
            Console.ResetColor();
        }

        Environment.Exit(0);
    }
}