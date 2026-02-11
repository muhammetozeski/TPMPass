namespace SecureTPMVault;

/// <summary>
/// Centralized color palette for the application.
/// Ensures consistency across UI elements.
/// </summary>
internal static class Theme
{
    // Notification Colors
    public static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
    public static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
    public static readonly ConsoleColor TipColor = ConsoleColor.Cyan;
    public static readonly ConsoleColor SuccessColor = ConsoleColor.Green;

    // UI Colors
    public static readonly ConsoleColor HeaderColor = ConsoleColor.Magenta;
    public static readonly ConsoleColor TextColor = ConsoleColor.White;
    public static readonly ConsoleColor DimTextColor = ConsoleColor.DarkGray;
    public static readonly ConsoleColor BorderColor = ConsoleColor.Gray;
}