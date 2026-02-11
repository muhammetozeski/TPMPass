using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace SecureTPMVault;

internal static class KeyManager
{
    private static readonly byte[] _staticEntropy = [0x45, 0x12, 0x99, 0xAD, 0x11, 0x33, 0xFD, 0xCB, 0x22, 0x55, 0x88, 0xAA, 0x00, 0xFF, 0x12, 0x34];

    /// <summary>
    /// The length of the file salt used for key derivation (32 bytes).
    /// </summary>
    public const int FileSaltLength = 32;

    private static SecureBuffer? _secureMasterKey;

    private static string MasterKeyFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppConstants.AppTitle);
    private static string MasterKeyPath => Path.Combine(MasterKeyFolderPath, AppConstants.MasterKeyFile);
    private static string InfoFilePath => Path.Combine(MasterKeyFolderPath, "READ_ME_CRITICAL.txt");

    public static void InitializeMasterKey()
    {
        if (!Directory.Exists(MasterKeyFolderPath)) Directory.CreateDirectory(MasterKeyFolderPath);

        if (!File.Exists(InfoFilePath))
            File.WriteAllText(InfoFilePath, VaultInfo.MasterKeyWarning);

        if (File.Exists(MasterKeyPath))
        {
            try
            {
                byte[] encryptedFileContent = File.ReadAllBytes(MasterKeyPath);

                // DPAPI Unprotect (Disk -> RAM Plaintext)
                byte[] rawKey = ProtectedData.Unprotect(encryptedFileContent, _staticEntropy, DataProtectionScope.CurrentUser);

                // Immediately wrap in SecureBuffer (RAM Plaintext -> RAM Encrypted)
                _secureMasterKey = new SecureBuffer(rawKey);

                // WIPE the raw arrays immediately
                Array.Clear(rawKey);
                Array.Clear(encryptedFileContent);
            }
            catch (Exception ex)
            {
                NotificationService.Add($"Master Key corruption detected ({ex.Message}). Generating new identity...", NotificationType.Warning);
                GenerateAndSaveNewMasterKey();
            }
        }
        else
        {
            GenerateAndSaveNewMasterKey();
        }
    }

    /// <summary>
    /// Derives final entropy by temporarily unlocking the master key in RAM.
    /// </summary>
    public static byte[] DeriveFinalEntropy(byte[] fileSpecificSalt)
    {
        if (_secureMasterKey is null) throw new InvalidOperationException("Master Key not initialized.");

        byte[]? resultHash = null;

        // SecureBuffer handles the Decrypt -> Action -> Encrypt cycle
        _secureMasterKey.AccessData(rawMasterKey =>
        {
            // Combined: Static Entropy + User Master Key + File Salt
            // We do this inside the AccessData scope where rawMasterKey is briefly visible
            byte[] combined = [.. _staticEntropy, .. rawMasterKey, .. fileSpecificSalt];

            resultHash = SHA256.HashData(combined);

            // Clear the combined buffer immediately
            Array.Clear(combined);
        });

        return resultHash ?? throw new CryptographicException("Failed to derive key.");
    }

    public static byte[] GenerateFileSalt()
    {
        byte[] salt = new byte[FileSaltLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private static void GenerateAndSaveNewMasterKey()
    {
        byte[] newKey = new byte[32];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(newKey);

        // 1. Save to SecureBuffer (RAM Protection)
        _secureMasterKey = new SecureBuffer(newKey);

        // 2. Encrypt and Save to Disk (Disk Protection)
        byte[] toSave = ProtectedData.Protect(newKey, _staticEntropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(MasterKeyPath, toSave);

        // Wipe raw key from this method's scope
        Array.Clear(newKey);

        File.WriteAllText(InfoFilePath, VaultInfo.MasterKeyWarning);
    }
}