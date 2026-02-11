namespace SecureTPMVault;

internal static class FileService
{
    /// <summary>
    /// Resolves the full path for a given input path.
    /// If the path is empty, returns the default path.
    /// If the extension is missing, appends the default extension.
    /// </summary>
    /// <param name="inputPath">The user provided path or filename.</param>
    /// <returns>The absolute file path.</returns>
    public static string ResolvePath(string? inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            return Path.Combine(AppConstants.ThisExeFolder, AppConstants.DefaultEncryptedFile);

        if (!Path.HasExtension(inputPath))
            inputPath += AppConstants.FileExtension;

        if (!Path.IsPathFullyQualified(inputPath))
            inputPath = Path.Combine(AppConstants.ThisExeFolder, inputPath);
        
        return inputPath;
    }
}