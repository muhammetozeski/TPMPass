namespace SecureTPMVault;
/// <summary>
/// Represents a command-line argument definition with a long and short alias.
/// </summary>
/// <param name="longName">The full name of the argument (e.g., --help).</param>
/// <param name="shortName">The short alias of the argument (e.g., -h).</param>
internal class CmdArg(string longName, string shortName)
{
    /// <summary>
    /// The full command name (e.g., --set).
    /// </summary>
    public readonly string LongName = longName;

    /// <summary>
    /// The short command alias (e.g., -s).
    /// </summary>
    public readonly string ShortName = shortName;

    /// <summary>
    /// Checks if the provided input matches either the long name or the short name.
    /// </summary>
    /// <param name="input">The argument string to check.</param>
    /// <returns>True if the input matches; otherwise, false.</returns>
    public bool IsMatch(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return input.Equals(LongName, StringComparison.OrdinalIgnoreCase) ||
               input.Equals(ShortName, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class ArgumentDef
{
    public static readonly CmdArg Set = new("--set", "-s");
    public static readonly CmdArg Get = new("--get", "-g");
    public static readonly CmdArg NoUI = new("--noUI", "-n");
}

internal static class CommandProcessor
{
    /// <summary>
    /// Parses and executes the command line arguments.
    /// </summary>
    public static void Execute(string[] args)
    {
        string primaryCommand = args[0];
        bool noUI = args.Any(arg => ArgumentDef.NoUI.IsMatch(arg));

        try
        {
            if (ArgumentDef.Set.IsMatch(primaryCommand))
            {
                if (args.Length < 2) return;
                string? outputPath = args.Length > 2 ? args[2] : null;
                CryptoService.EncryptAndSave(args[1], FileService.ResolvePath(outputPath));
            }
            else if (ArgumentDef.Get.IsMatch(primaryCommand))
            {
                if (args.Length < 2) return;
                CryptoService.DecryptAndPrint(FileService.ResolvePath(args[1]), false);
            }
            else if (File.Exists(primaryCommand))
            {
                // If the argument is a file, default to decrypt mode
                CryptoService.DecryptAndInteract(primaryCommand, noUI);
            }
            else
                Console.WriteLine("Unknown command");
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Execution failed: {ex.Message}", ConsoleColor.Red);
        }
    }
}