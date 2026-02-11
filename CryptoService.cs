using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace SecureTPMVault;

internal static class CryptoService
{
    // CancellationTokenSource, CopyDataWithSingletonTimer, ManualClearClipboard ve PerformAutoSequence bu sınıftan kaldırıldı.

    /// <summary>
    /// Encrypts the provided string and saves it to the specified file path using a generated salt.
    /// Uses NotificationService to report success or failure.
    /// </summary>
    /// <param name="plainText">The data string to be encrypted.</param>
    /// <param name="filePath">The absolute path where the encrypted file will be saved.</param>
    public static void EncryptAndSave(string plainText, string filePath)
    {
        try
        {
            byte[] fileSalt = KeyManager.GenerateFileSalt();
            byte[] finalEntropy = KeyManager.DeriveFinalEntropy(fileSalt);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] encryptedData = ProtectedData.Protect(plainBytes, finalEntropy, DataProtectionScope.CurrentUser);

            Array.Clear(plainBytes);
            Array.Clear(finalEntropy);

            using var fs = new FileStream(filePath, FileMode.Create);
            fs.Write(fileSalt);
            fs.Write(encryptedData);

            NotificationService.Add($"Data secured at: {Path.GetFileName(filePath)}", NotificationType.Success);
        }
        catch (Exception ex)
        {
            NotificationService.Add($"Encryption failed: {ex.Message}", NotificationType.Error);
        }
    }

    /// <summary>
    /// Decrypts the specified file content and returns the protected data wrapped in a SecureBuffer.
    /// This function handles the core cryptographic process and does not interact with the UI or clipboard.
    /// </summary>
    /// <param name="filePath">Path to the encrypted file.</param>
    /// <returns>A SecureBuffer object containing the decrypted password data, protected in RAM.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown if the file format is incorrect or corrupt.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails.</exception>
    public static SecureBuffer DecryptFile(string filePath)
    {
        const int FileSaltLength = KeyManager.FileSaltLength;

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        byte[] fileBytes = File.ReadAllBytes(filePath);

        if (fileBytes.Length <= FileSaltLength)
            throw new InvalidDataException("Encrypted file is too short or corrupt.");

        byte[] fileSalt = new byte[FileSaltLength];
        byte[] encryptedData = new byte[fileBytes.Length - FileSaltLength];

        Array.Copy(fileBytes, fileSalt, FileSaltLength);
        Array.Copy(fileBytes, FileSaltLength, encryptedData, 0, encryptedData.Length);

        byte[] finalEntropy = KeyManager.DeriveFinalEntropy(fileSalt);

        byte[] decryptedBytes = ProtectedData.Unprotect(encryptedData, finalEntropy, DataProtectionScope.CurrentUser);

        Array.Clear(fileSalt);
        Array.Clear(finalEntropy);
        Array.Clear(encryptedData);
        Array.Clear(fileBytes);

        return new SecureBuffer(decryptedBytes);
    }

    /// <summary>
    /// Coordinates the decryption process, performs security checks, and delegates the result 
    /// to the appropriate UI/Interaction handler.
    /// </summary>
    /// <param name="filePath">Path to the encrypted file.</param>
    /// <param name="noUI">If true, delegates handling to the non-interactive CLI handler.</param>
    public static void DecryptAndInteract(string filePath, bool noUI)
    {
        SecureBuffer? secureData = null;
        try
        {
            // Perform security scan after successful decryption
            if (SecurityUtils.ScanCallerProcess())
            {
                NotificationService.Add("ACCESS DENIED: Security threat detected.", NotificationType.Error);
                return; // Let finally block dispose the buffer
            }

            secureData = DecryptFile(filePath);

            if (noUI)
            {
                UIManager.HandleDecryptedDataNoUI(secureData);
            }
            else
            {
                UIManager.ShowVaultScreen(secureData, filePath);
            }
        }
        catch (Exception ex)
        {
            NotificationService.Add($"Decryption failed: {ex.Message}", NotificationType.Error);
        }
        finally
        {
            secureData?.Dispose();
        }
    }

    /// <summary>
    /// Decrypts and prints the content directly to the standard output. 
    /// Intended for CLI piping/scripts where UI notifications are not desired.
    /// </summary>
    /// <param name="filePath">Path to the encrypted file.</param>
    /// <param name="isInteractive">If true, prints a newline after the content (used for simple 'get' commands).</param>
    public static void DecryptAndPrint(string filePath, bool isInteractive)
    {
        SecureBuffer? secureData = null;
        try
        {
            secureData = DecryptFile(filePath);

            secureData.AccessData(plainDataBytes =>
            {
                string plainText = Encoding.UTF8.GetString(plainDataBytes);

                if (isInteractive) Console.WriteLine(plainText);
                else Console.Write(plainText);
            });
        }
        catch (Exception ex)
        {
            // For CLI piping, standard error stream is preferred over UI notifications
            Console.Error.WriteLine($"[ERROR] Decryption failed: {ex.Message}");
        }
        finally
        {
            secureData?.Dispose();
        }
    }
}