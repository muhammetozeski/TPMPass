using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SecureTPMVault;

internal static class UIManager
{
    // Clipboard Management Members (Moved from CryptoService for SRP)
    private static CancellationTokenSource? _clipboardCts;

    /// <summary>
    /// Displays the main dashboard menu loop.
    /// Manages user navigation between Encryption, Decryption, Settings, and Exit.
    /// </summary>
    public static void ShowMainMenu()
    {
        Console.Title = AppConstants.AppTitle;
        while (true)
        {
            Console.Clear();
            PrintHeader();

            // Renders the notification queue (Errors, Warnings, Tips)
            NotificationService.RenderNotifications();
            Console.WriteLine();

            Log(" [1] ", Theme.TipColor, true, false, false); Console.WriteLine("Encrypt New Password");
            Log(" [2] ", Theme.TipColor, true, false, false); Console.WriteLine("Decrypt Password File");
            Log(" [3] ", Theme.TipColor, true, false, false); Console.WriteLine("Settings");
            Log(" [4] ", Theme.TipColor, true, false, false); Console.WriteLine("Clear Clipboard");
            Log(" [5] ", Theme.ErrorColor, true, false, false); Console.WriteLine("Exit");

            Console.Write("\n>> ");
            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.D1: case ConsoleKey.NumPad1: UIEncryptFlow(); break;
                case ConsoleKey.D2: case ConsoleKey.NumPad2: UIDecryptFlow(); break;
                case ConsoleKey.D3: case ConsoleKey.NumPad3: UISettingsFlow(); break;
                case ConsoleKey.D4: case ConsoleKey.NumPad4: ClearClipboardAndStopTimer(); break;
                case ConsoleKey.D5: case ConsoleKey.NumPad5: case ConsoleKey.Escape: return;
            }
        }
    }

    /// <summary>
    /// Starts a timer to copy data to the clipboard, clearing it after the configured delay.
    /// Cancels any previous timer to ensure only one is active at a time (Singleton).
    /// </summary>
    /// <param name="secureData">The SecureBuffer containing the data to copy.</param>
    private static void CopyDataWithSingletonTimer(SecureBuffer secureData)
    {
        _clipboardCts?.Cancel();
        _clipboardCts = new CancellationTokenSource();
        CancellationToken token = _clipboardCts.Token;

        // Corrected: Data is accessed as byte[], converted to string (UTF8), and then sent to ClipboardService.
        // This conversion occurs securely within the AccessData scope.
        secureData.AccessData(bytes =>
        {
            ClipboardService.SetText(Encoding.UTF8.GetString(bytes));
        });

        if (Settings.ClipboardClearSeconds <= 0) return;

        Task.Delay(TimeSpan.FromSeconds(Settings.ClipboardClearSeconds), token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;

            try
            {
                ClipboardService.SetText(string.Empty);
                NotificationService.Add("Clipboard cleared automatically.", NotificationType.Success);
            }
            catch (Exception)
            {
                // Warn if clearing fails, usually because another application holds the clipboard lock.
                NotificationService.Add("Auto-clipboard clear failed (clipboard busy).", NotificationType.Warning);
            }
        }, token);
    }

    /// <summary>
    /// Manually clears the clipboard immediately and stops any pending background timers.
    /// </summary>
    public static void ClearClipboardAndStopTimer()
    {
        _clipboardCts?.Cancel();
        ClipboardService.SetText(string.Empty);
        NotificationService.Add("Clipboard cleared successfully.", NotificationType.Success);
    }

    ///// <summary>
    ///// Handles the interactive display and auto-copy sequence for decrypted data.
    ///// This is the auto-sequence used when Settings.AutoReveal is true.
    ///// </summary>
    ///// <param name="secureData">The protected decrypted data.</param>
    ///// <param name="filePath">The source file path.</param>
    //public static void HandleDecryptedDataInteractive(SecureBuffer secureData, string filePath)
    //{
    //    CopyDataWithSingletonTimer(secureData);
    //    if (Settings.ShowNotifications)
    //        NotificationService.Add("Data copied to buffer automatically.", NotificationType.Success);

    //    Console.Clear();
    //    PrintHeader();
    //    NotificationService.RenderNotifications();

    //    Log($"Target: {Path.GetFileName(filePath)}", Theme.TipColor, WriteToDisk: false);
    //    Console.WriteLine("----------------------------------");
    //    Log("PASSWORD:", Theme.ErrorColor, WriteToDisk: false);

    //    secureData.AccessData(bytes =>
    //    {
    //        Console.ForegroundColor = Theme.TextColor;
    //        Console.WriteLine(" " + Encoding.UTF8.GetString(bytes) + " ");
    //        Console.ResetColor();
    //    });

    //    Console.WriteLine("----------------------------------");
    //    Log("\nPress any key to return...", Theme.DimTextColor, WriteToDisk: false);
    //    Console.ReadKey();
    //}

    /// <summary>
    /// Handles the non-interactive (CLI) auto-copy for decrypted data.
    /// </summary>
    /// <param name="secureData">The protected decrypted data.</param>
    public static void HandleDecryptedDataNoUI(SecureBuffer secureData)
    {
        CopyDataWithSingletonTimer(secureData);
    }


    /// <summary>
    /// Displays the vault interaction screen where the user can Reveal, Copy, or Clear the decrypted password.
    /// </summary>
    /// <param name="secureData">The protected decrypted data.</param>
    /// <param name="filePath">The path of the file being viewed.</param>
    public static void ShowVaultScreen(SecureBuffer secureData, string filePath)
    {
        bool copied = Settings.AutoCopyToClipboard;
        if (Settings.AutoCopyToClipboard)
        {
            CopyDataWithSingletonTimer(secureData);
            if (Settings.ShowNotifications)
                NotificationService.Add("Data copied to buffer automatically.", NotificationType.Success);
        }

        bool reveal = Settings.AutoRevealPassword;
        string? tempMessage = copied && Settings
.ShowNotifications
            ? "Password copied automatically." : null;

        while (true)
        {
            Console.Clear();
            PrintHeader();
            NotificationService.RenderNotifications();

            Log($"File: {Path.GetFileName(filePath)}", Theme.TipColor, WriteToDisk: false);
            Console.WriteLine("----------------------------------");

            if (reveal)
            {
                Log("PASSWORD:", Theme.ErrorColor, WriteToDisk: false);
                secureData.AccessData(bytes =>
                {
                    Console.ForegroundColor = Theme.TextColor;
                    Console.WriteLine(" " + Encoding.UTF8.GetString(bytes) + " ");
                    Console.ResetColor();
                });
                Console.WriteLine("----------------------------------");
                reveal = false;
            }

            if (tempMessage != null)
            {
                Log($"[INFO] {tempMessage}", Theme.SuccessColor, WriteToDisk: false);
                tempMessage = null;
                Console.WriteLine();
            }
            if(!Settings.AutoRevealPassword)
                Console.WriteLine(" [1] Reveal Password");
            if (!Settings.AutoCopyToClipboard)
                Console.WriteLine(" [2] Copy to Clipboard");
            if(copied)
                Console.WriteLine(" [3] Clear Clipboard Now");
            Console.WriteLine(" [4] Back / Exit");

            Console.Write("\nSelect >> ");
            var key = Console.ReadKey(true);

            if (key.KeyChar=='1')
            {
                reveal = true;
            }
            else if (key.KeyChar =='2')
            {
                CopyDataWithSingletonTimer(secureData);
                if (Settings.ShowNotifications)
                {
                    tempMessage = "Copied successfully.";
                    copied = true;
                }
            }
            else if (key.KeyChar == '3')
            {
                ClearClipboardAndStopTimer();
                tempMessage = "Clipboard cleared.";
                copied = false;
            }
            else if (key.KeyChar == '4'|| key.Key == ConsoleKey.Escape)
            {
                break;
            }
        }
    }

    private static void UIEncryptFlow()
    {
        Console.Clear(); PrintHeader();
        Log("--- ENCRYPTION MODE ---", Theme.SuccessColor, WriteToDisk: false);
        Console.Write("Password to Encrypt: ");
        string pass = ReadPassword();
        if (string.IsNullOrWhiteSpace(pass)) return;

        Console.Write($"Output Filename (Default: {AppConstants.DefaultEncryptedFile}): ");
        string? input = Console.ReadLine();
        string path = FileService.ResolvePath(input);
        string fileName = Path.GetFileName(path);
        if (File.Exists(path))
        {
            Console.WriteLine(path + " is already exist. Do you want to overwrite on it? Please choose:\n" +
                "[1] Yes\n[2] No");
            bool OverWrite = ReadKey((i) => i.KeyChar == '1');
            if (OverWrite)
                CryptoService.EncryptAndSave(pass, path);
            else
                NotificationService.Add(fileName + " isn't overwritten.", NotificationType.Info);
        }
        else
            CryptoService.EncryptAndSave(pass, path);
    }

    private static void UIDecryptFlow()
    {
        Console.Clear(); PrintHeader();
        Log("--- DECRYPTION MODE ---", Theme.WarningColor, WriteToDisk: false);
        Console.Write($"Input Filename (Default: {AppConstants.DefaultEncryptedFile}): ");
        string? input = Console.ReadLine();

        CryptoService.DecryptAndInteract(FileService.ResolvePath(input), false);
    }
    /// <summary>
    /// Displays the settings menu dynamically by iterating through registered settings.
    /// It handles user input to toggle boolean values or update integer values automatically.
    /// </summary>
    private static void UISettingsFlow()
    {
        // Retrieve all registered settings as objects.
        var allSettings = SettingsManager.GetAllSettings();

        // Define the specific key that requires 's' suffix, avoiding magic strings.
        const string ClearDelayKey = nameof(Settings.ClipboardClearSeconds);

        while (true)
        {
            Console.Clear();
            PrintHeader();
            Log("--- SETTINGS ---", Theme.HeaderColor, WriteToDisk: false);

            // 1. Menu Rendering
            for (int i = 0; i < allSettings.Length; i++)
            {
                ISetting setting = allSettings[i];

                string displayName = setting.KeyHumanReadable;

                string? displayValue = setting.Value.ToString();

                // Specific exception for ClipboardClearSeconds key
                if (setting.Key == ClearDelayKey)
                    displayValue += "s";

                Console.WriteLine($" [{i + 1}] {displayName,-25}: {displayValue}");
            }

            Console.WriteLine(" [0] Back");

            // 2. Input Handling
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            // Handle Back/Escape control flow first
            if (keyInfo.KeyChar == '0' || keyInfo.Key == ConsoleKey.Escape)
            {
                SettingsManager.SaveSettings();
                return;
            }

            // Check if the input is a digit corresponding to a setting index
            if (char.IsDigit(keyInfo.KeyChar))
            {
                // Convert char '1' to index 0
                if (int.TryParse(keyInfo.KeyChar.ToString(), out int selectionIndex) && selectionIndex > 0)
                {
                    int index = selectionIndex - 1;

                    if (index >= 0 && index < allSettings.Length)
                    {
                        var selectedSetting = allSettings[index];

                        // 3. Pattern Matching for Type-Specific Logic
                        if (selectedSetting is Setting<bool> boolSetting)
                        {
                            // Toggle boolean value directly
                            boolSetting.Value = !boolSetting.Value;
                            SettingsManager.SaveSettings();
                        }
                        else if (selectedSetting is Setting<int> intSetting)
                        {
                            // Handle integer input with TryParse error control
                            Console.Write($"\nEnter new value for {HelperFunctions.SplitCamelCase(intSetting.Key)} (Seconds, 0=Disable): ");
                            string? input = Console.ReadLine();

                            // TryParse + validation for non-negative numbers
                            if (int.TryParse(input, out int parsedVal) && parsedVal >= 0)
                            {
                                intSetting.Value = parsedVal;
                                SettingsManager.SaveSettings();
                            }
                            // If TryParse fails or input is negative, no action is taken, and the loop continues.
                        }
                        // Add other types here if needed
                    }
                }
            }
            // debug purpose:
            Debug.WriteLine("Settings.AutoCopyToClipboard: " + Settings.AutoCopyToClipboard.Value);
            Debug.WriteLine("Settings.ShowNotifications: " + Settings.ShowNotifications.Value);
            Debug.WriteLine("Settings.AutoRevealPassword: " + Settings.AutoRevealPassword.Value);
            Debug.WriteLine("Settings.ClipboardClearSeconds: " + Settings.ClipboardClearSeconds.Value);
        }
    }

    public static void PrintHeader()
    {
        Log("  _______ _____  __  __   ", ConsoleColor.DarkRed, WriteToDisk: false);
        Log(" |__   __|  __ \\|  \\/  |  ", ConsoleColor.Red, WriteToDisk: false);
        Log("    | |  | |__) | \\  / |  ", ConsoleColor.Magenta, WriteToDisk: false);
        Log("    | |  |  ___/| |\\/| |  ", ConsoleColor.DarkMagenta, WriteToDisk: false);
        Log("    | |  | |    | |  | |  ", ConsoleColor.Blue, WriteToDisk: false);
        Log("    |_|  |_|    |_|  |_|  ", ConsoleColor.DarkBlue, WriteToDisk: false);
        Log("   SECURE PASSWORD VAULT  ", ConsoleColor.DarkGray, WriteToDisk: false);

        Console.WriteLine();
    }

    private static string ReadPassword()
    {
        string pass = "";
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (pass.Length > 0) { pass = pass[0..^1]; Console.Write(AppConstants.PasswordErase); }
            }
            else
            {
                pass += key.KeyChar;
                Console.Write(AppConstants.PasswordMask);
            }
        }
        Console.WriteLine();
        return pass;
    }
}