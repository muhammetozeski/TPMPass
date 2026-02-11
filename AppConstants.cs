namespace SecureTPMVault;

/// <summary>
/// Contains global constants and configuration defaults used throughout the application.
/// </summary>
internal static class AppConstants
{
    public const bool PublishMode = true;

    /// <summary>
    /// DEVELOPER SETTING: If true, the application will terminate immediately if a debugger is attached.
    /// </summary>
    public const bool EnableAntiDebug = PublishMode;

    /// <summary>
    /// DEVELOPER SETTING: If true, the application forces itself to restart as Administrator.
    /// </summary>
    public const bool EnableForceAdmin = false;

#pragma warning disable CS8601 // Possible null reference assignment.
    public static readonly string ThisExePath = Environment.ProcessPath; // This won't be null for file-based executables
    public static readonly string ThisExeFolder = Path.GetDirectoryName(ThisExePath);
#pragma warning restore CS8601 // Possible null reference assignment.

    /// <summary>
    /// The name of the configuration file.
    /// </summary>
    public const string ConfigFileName = "config.txt";

    /// <summary>
    /// The filename for the user's master key.
    /// </summary>
    public const string MasterKeyFile = "user_master.dat";

    /// <summary>
    /// The default filename for encrypted password storage.
    /// </summary>
    public const string DefaultEncryptedFile = "main.tpmPassword";

    /// <summary>
    /// The enforced file extension for encrypted files.
    /// </summary>
    public const string FileExtension = ".tpmPassword";

    /// <summary>
    /// Prefix used for comments in the configuration file.
    /// </summary>
    public const string CommentPrefix = "#";

    /// <summary>
    /// Separator used for key-value pairs in the configuration file.
    /// </summary>
    public const string KeyValueSeparator = "=";

    /// <summary>
    /// The application window title.
    /// </summary>
    public const string AppTitle = "TPM Secure Vault";

    /// <summary>
    /// Mask character used during password input.
    /// </summary>
    public const string PasswordMask = "*";

    /// <summary>
    /// Sequence used to erase a character from the console (Backspace + Space + Backspace).
    /// </summary>
    public const string PasswordErase = "\b \b";
}