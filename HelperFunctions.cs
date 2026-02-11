using System.Diagnostics;

// TODO: split these functions to different classes according to solid principles
internal static class HelperFunctions
{
    /// <summary>
    /// Converts a camelCase or PascalCase string into human-readable words by
    /// inserting spaces before capital letters and numbers.
    /// </summary>
    /// <param name="input">The camelCase or PascalCase string (e.g., AutoCopyToClipboard).</param>
    /// <returns>The human-readable string (e.g., Auto Copy To Clipboard).</returns>
    public static string SplitCamelCase(string input)
    {
        // Inserts a space before every capital letter (A-Z) or number (0-9)
        // that is followed by a non-capital letter (a-z) or number.
        // This is the most efficient and concise way to achieve the goal using Regex.
        return System.Text.RegularExpressions.Regex.Replace(
            input,
            "([A-Z0-9])",
            " $1"
        ).Trim();
    }
    /// <summary>
    /// Opens the given URL in the default browser.
    /// SRP: Sole responsibility for opening the URL in the browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    public static void OpenUrlInBrowser(string url)
    {
        Log("Opening playlist in default browser...");
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"An error occurred while opening the browser: {ex.Message}");
        }
    }
    public static void OpenWithDefaultProgram(string path)
    {
        using Process fileopener = new();

        fileopener.StartInfo.FileName = "explorer";
        fileopener.StartInfo.Arguments = "\"" + path + "\"";
        fileopener.Start();
    }

    public static bool IsPathValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // UNC yollarını ve uzun yol prefix'lerini reddet
        if (path.StartsWith(@"\\") || path.StartsWith(@"\\?\"))
            return false;

        try
        {
            // Geçersiz path karakterlerini kontrol et
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // Root'lu bir yol mu?
            if (!Path.IsPathRooted(path))
                return false;

            string root = Path.GetPathRoot(path)!;

            // Root formatını kontrol et (en az 3 karakter, örn: "C:\")
            if (root.Length < 3 || root[1] != ':' || root[2] != '\\')
                return false;

            // Kalan kısmı kontrol et
            string remaining = path[root.Length..];
            string[] parts = remaining.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue; // Çift ters slash'ları görmezden gel

                // Geçersiz dosya adı karakterleri ve boşluk kontrolü
                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.Trim() == "")
                    return false;
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static async Task<bool> DownloadFileAsync(string fileUrl, string? destinationPath = null, Action? BeforeStartDownload = null, int LogDelay = 500, int BufferSize = 8192, CancellationToken cancellationToken = default)
    {
        var stepStopwatch = Stopwatch.StartNew();
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            Log("Attempting to download file from: " + fileUrl);

            using var client = new HttpClient();
            Log("Sending GET request to: " + fileUrl);
            var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Log("GET request done.\nTime taken: " + stepStopwatch.Elapsed);
            stepStopwatch.Restart();

            Log("Checking HTTP response status code.");
            response.EnsureSuccessStatusCode();
            Log("HTTP response status code check successful.\nTime taken: " + stepStopwatch.Elapsed);
            stepStopwatch.Restart();

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                Log("No destination path provided. Attempting to get filename from Content-Disposition.");
                var contentDisposition = response.Content.Headers.ContentDisposition;
                if (contentDisposition?.FileName != null)
                {
                    destinationPath = contentDisposition?.FileName;
                    Log("destinationPath = contentDisposition?.FileName;");
                }
                else
                {
                    destinationPath = Path.GetFileName(new Uri(fileUrl).LocalPath);
                    Log("destinationPath = Path.GetFileName(new Uri(fileUrl).LocalPath);");
                }

                destinationPath ??= "DownloadedFile_" + Guid.NewGuid() + ".bin";

                Log("Destination path set to: " + destinationPath);
                Log("Destination path determined.\nTime taken: " + stepStopwatch.Elapsed);
                stepStopwatch.Restart();
            }
            else
            {
                Log("Destination path provided: " + destinationPath);
                Log("Destination path determined.\nTime taken: " + stepStopwatch.Elapsed);
                stepStopwatch.Restart();
            }

            long totalBytesRead = 0;
            Log("Reading file content as stream.");
            using (var streamToReadFrom = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                long fileSize = response.Content.Headers.ContentLength ?? -1;
                string fileSizeFormatted = FormatBytes(fileSize);
                Log("Total file size: " + FormatBytes(fileSize));

                using var streamToWriteTo = System.IO.File.Open(destinationPath, FileMode.Create);
                byte[] buffer = new byte[BufferSize];
                int bytesRead;
                long lastLogTime = 0;
                var downloadStopwatch = Stopwatch.StartNew();
                long lastDownloadedFileSize = 0;

                BeforeStartDownload?.Invoke();

                Console.WriteLine("Downloading:");

                while ((bytesRead = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await streamToWriteTo.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;

                    long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (currentTime - lastLogTime > LogDelay)
                    {
                        lastLogTime = currentTime;

                        double percentage = (double)totalBytesRead / fileSize * 100;

                        var elapsed = downloadStopwatch.ElapsedMilliseconds;
                        long downloadSpeed = CalculateDownloadSpeed(totalBytesRead - lastDownloadedFileSize, elapsed);

                        string _write = "" + percentage.ToString("F2") + "%, " +
                            FormatBytes(totalBytesRead) + "/" + fileSizeFormatted + ", " +
                            "Download speed: " + FormatBytes(downloadSpeed) + " / sec" +
                            ", Elapsed milliseconds: " + elapsed + ", Now Downloaded: " + FormatBytes(totalBytesRead - lastDownloadedFileSize);

                        Console.Write("\r" + _write + "        ");

                        lastDownloadedFileSize = totalBytesRead;
                        downloadStopwatch.Restart();
                    }
                }
                Log("File content written to destination.\nTime taken: " + downloadStopwatch.Elapsed);
            }
            Log("File downloaded successfully: " + destinationPath +
                "\nTotal file size: " + FormatBytes(totalBytesRead));
            return true;
        }
        catch (OperationCanceledException)
        {
            Log("OperationCanceledException: Download cancelled by user.");
            return false;
        }
        catch (HttpRequestException e)
        {
            Log("HttpRequestException: Error downloading file from " + fileUrl + "\n\nException message:\n" + e);
            return false;
        }
        catch (UriFormatException e)
        {
            Log("UriFormatException: Invalid URL format: " + fileUrl + "\n\nException message:\n" + e);
            return false;
        }
        catch (IOException e)
        {
            Log("IOException: Error writing file to " + destinationPath + "\n\nException message:\n" + e);
            return false;
        }
        catch (Exception e)
        {
            Log("An unexpected error occurred:\n\nException message:\n" + e);
            return false;
        }
        finally
        {
            Log("Total download time: " + totalStopwatch.Elapsed);
        }
    }


    public static long CalculateDownloadSpeed(long bytesDownloaded, long timeElapsedMilliseconds)
    {
        if (timeElapsedMilliseconds == 0) return 0;
        return (bytesDownloaded * 1000) / timeElapsedMilliseconds;
    }
    public static string FormatBytes(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        if (bytes < KB)
            return $"{bytes} B";
        else if (bytes < MB)
            return $"{(double)bytes / KB:F2} KB";
        else if (bytes < GB)
            return $"{(double)bytes / MB:F2} MB";
        else if (bytes < TB)
            return $"{(double)bytes / GB:F2} GB";
        else
            return $"{(double)bytes / TB:F2} TB";
    }

    public static bool IsInSameFolder(string filePath, string folderPath) =>
    Path.GetDirectoryName(Path.GetFullPath(filePath)) == Path.GetFullPath(folderPath);

    #region ReadLine
    /// <summary> This will put wait emoji into the title if the "ChangeTitleEmoji" in the <see cref="ReadLine{T}(Func{string, T}, bool, bool?)"/> is not defined </summary>
    public static bool ChangeTitleEmojiOnReadlineByDefault = true;

    /// <summary>
    ///example usage:
    ///<code>
    ///int index = 0;
    ///ReadLineNonNull((input) =>
    ///{
    ///    index = int.Parse(input);
    ///    if (index &gt; menu.Length - 1 || index &lt; 0)
    ///        throw new Exception("The input can't be less or higher than the menu element amount.");
    ///});
    /// </code>
    /// </summary>
    public static T ReadLine<T>(Func<string, T> returnable, bool TryAgainIfNotNull = true, bool? ChangeTitleEmoji = null)
    {
        while (true)
            try
            {
                string? input;
                if (ChangeTitleEmoji == null)
                {
                    if (ChangeTitleEmojiOnReadlineByDefault)
                    {
                        Console.Title = "❓" + Console.Title[1..];
                        input = Console.ReadLine();
                        Console.Title = "⏳" + Console.Title[1..];
                    }
                    else
                        input = Console.ReadLine();
                }
                else
                {
                    if (ChangeTitleEmoji.Value)
                        Console.Title = "❓" + Console.Title[1..];
                    input = Console.ReadLine();
                    if (ChangeTitleEmoji.Value)
                        Console.Title = "⏳" + Console.Title[1..];
                }

                if (TryAgainIfNotNull && string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("The input is empty. Please enter the input again:");
                    continue;
                }

                return returnable(input);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured. Error message: " + e.Message);
                Console.WriteLine("\nPlease enter the input again:");
            }
    }

    /// <summary> <inheritdoc cref="ReadLine{T}(Func{string, T}, bool, bool)"/> </summary>
    public static string ReadLine(Action<string> action, bool TryAgainIfNotNull = true, bool ChangeTitleEmoji = true)
    {
        return ReadLine((input) =>
        {
            action(input);
            return input;
        }, TryAgainIfNotNull, ChangeTitleEmoji);
    }

    /// <summary> <inheritdoc cref="ReadLine{T}(Func{string, T}, bool, bool)"/> </summary>
    public static string ReadLine(bool TryAgainIfNotNull = true, bool ChangeTitleEmoji = true)
    {
        return ReadLine((input) =>
        {
            return input;
        }, TryAgainIfNotNull, ChangeTitleEmoji);
    }


    #endregion

    #region ReadKey

    /// <summary>
    /// Ensures a valid key is pressed and processed correctly.<br></br>
    /// Example usage:
    /// <code>
    /// ConsoleKey key = ReadKeyNonNull((input) =>
    /// {
    ///     if (input != ConsoleKey.A)
    ///         throw new Exception("Only A key is allowed.");
    ///     return input;
    /// });
    /// </code>
    /// </summary>
    public static ReturnType ReadKey<ReturnType>(Func<ConsoleKeyInfo, ReturnType> returnable, bool changeTitleEmoji = true)
    {
        if (changeTitleEmoji)
            Console.Title = "❓" + Console.Title[1..];
        while (true)
            try
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (changeTitleEmoji)
                    Console.Title = "⏳" + Console.Title[1..];
                return returnable(key);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred. Error message: " + e.Message);
                Console.WriteLine("Please press a key again:");
            }
    }

    /// <summary>
    /// Processes a key with an action and returns it.<br></br>
    /// Example: ReadKeyNonNull(k => { if (k != ConsoleKey.B) throw new Exception("Invalid"); });
    /// </summary>
    public static ConsoleKeyInfo ReadKey(Action<ConsoleKeyInfo> action, bool changeTitleEmoji = true)
    {
        return ReadKey((key) =>
        {
            action(key);
            return key;
        }, changeTitleEmoji);
    }

    public static ConsoleKeyInfo ReadKey(bool changeTitleEmoji = true)
    {
        return ReadKey((key) => key, changeTitleEmoji);
    }

    #endregion
    public static void DeleteOldestFiles(string folderPath, int filesToKeep, string prefix = "")
    {
        if (!Directory.Exists(folderPath))
        {
            Log("[ERROR] [DirectoryNotFoundException] File deletion operation: Folder not found. Folder path: " + folderPath);
            return; // Klasör bulunamazsa işlemden çık
        }

        var files = Directory.GetFiles(folderPath)
                             .Select(filePath => new
                             {
                                 Path = filePath,
                                 FileName = Path.GetFileNameWithoutExtension(filePath)
                             })
                             .ToList();

        var datedFiles = new List<(string Path, DateTime Date)>();

        foreach (var file in files)
        {
            string name = file.FileName;
            name = name.Contains(prefix) ? name.Remove(name.IndexOf(prefix), prefix.Length) : name;
            name = name.Trim();
            if (DateTime.TryParseExact(name, "yyyy.MM.dd HH.mm.ss.ff",
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None, out DateTime fileDate))
            {
                datedFiles.Add((file.Path, fileDate));
            }
            // Hatalı formatta dosya adları garanti edildiği için else bloğuna gerek yok.
        }

        var sortedFiles = datedFiles.OrderBy(f => f.Date).ToList();

        int filesToDeleteCount = sortedFiles.Count - filesToKeep;

        if (filesToDeleteCount <= 0)
        {
            return; // Silinecek dosya yok
        }

        for (int i = 0; i < filesToDeleteCount; i++)
        {
            File.Delete(sortedFiles[i].Path);
        }
    }
}
