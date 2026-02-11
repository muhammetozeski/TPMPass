namespace SecureTPMVault;

/// <summary>
/// The main entry point class for the application.
/// Orchestrates security checks, key initialization, and the main execution loop.
/// </summary>
internal class Program
{
    /// <summary>
    /// The main entry method.
    /// </summary>
    /// <param name="args">Command-line arguments provided at startup.</param>
    [STAThread]
    static void Main(string[] args)
    {
        // 1. SECURITY: Anti-Dump & Anti-Debug
        SecurityUtils.DisableCrashDumps();

        if (AppConstants.EnableAntiDebug && SecurityUtils.IsDebuggerAttached())
        {
            Environment.Exit(-1);
        }

        // 2. SECURITY: Admin Privilege Check
        if (AppConstants.EnableForceAdmin && !AdminHelper.IsRunningAsAdmin())
        {
            AdminHelper.RestartAsAdmin(args);
            return;
        }

        // 3. INITIALIZATION
        KeyManager.InitializeMasterKey();
        SettingsManager.LoadSettings();

        try
        {
            if (args.Length == 0)
            {
                UIManager.ShowMainMenu();
            }
            else
            {
                CommandProcessor.Execute(args);
            }
        }
        catch (Exception ex)
        {
            // Add critical error to notification system
            NotificationService.Add($"Critical Error: {ex.Message}", NotificationType.Error);

            // If in CLI mode, print directly as well since UI might not render
            if (args.Length > 0)
            {
                Console.ForegroundColor = Theme.ErrorColor;
                Console.WriteLine($"[CRITICAL] {ex.Message}");
                Console.ResetColor();
            }
            else
            {
                // Try to show UI to display the notification
                try
                {
                    UIManager.ShowMainMenu();
                }
                catch
                {
                    // Absolute fallback if UI crashes
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}