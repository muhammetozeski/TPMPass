using System.Runtime.InteropServices;

namespace SecureTPMVault;

/// <summary>
/// Provides low-level interaction with the Windows Clipboard using Source Generated P/Invokes.
/// </summary>
internal static partial class ClipboardService
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetClipboardData(uint uFormat, IntPtr hMem);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalLock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalFree(IntPtr hMem);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    /// <summary>
    /// Sets the clipboard text content directly using Win32 APIs.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard. If null, clipboard is cleared.</param>
    public static void SetText(string? text)
    {
        text ??= string.Empty;

        if (!OpenClipboard(IntPtr.Zero)) return;

        try
        {
            EmptyClipboard();
            if (string.IsNullOrEmpty(text)) return;

            int size = (text.Length + 1) * 2;
            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)size);

            if (hGlobal == IntPtr.Zero) return;

            try
            {
                IntPtr target = GlobalLock(hGlobal);
                if (target == IntPtr.Zero) return;

                try
                {
                    char[] chars = text.ToCharArray();
                    Marshal.Copy(chars, 0, target, chars.Length);
                    Marshal.WriteInt16(target + (chars.Length * 2), 0);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                if (!SetClipboardData(CF_UNICODETEXT, hGlobal))
                {
                    GlobalFree(hGlobal);
                }
                else
                {
                    hGlobal = IntPtr.Zero;
                }
            }
            catch
            {
                if (hGlobal != IntPtr.Zero) GlobalFree(hGlobal);
                throw;
            }
        }
        finally
        {
            CloseClipboard();
        }
    }
}