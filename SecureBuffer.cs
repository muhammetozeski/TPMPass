using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SecureTPMVault;

internal sealed partial class SecureBuffer : IDisposable
{
    [LibraryImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptProtectMemory([In, Out] byte[] pData, int cbData, uint dwFlags);

    [LibraryImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptUnprotectMemory([In, Out] byte[] pData, int cbData, uint dwFlags);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualLock(IntPtr lpAddress, UIntPtr dwSize);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualUnlock(IntPtr lpAddress, UIntPtr dwSize);

    private readonly byte[] _buffer;
    private readonly GCHandle _handle;
    private readonly int _dataLength;
    private bool _isDisposed;

    public SecureBuffer(byte[] plainData)
    {
        if (plainData is null || plainData.Length == 0)
            throw new ArgumentException("Buffer cannot be empty.", nameof(plainData));

        _dataLength = plainData.Length;
        int paddedSize = (plainData.Length + 15) / 16 * 16;
        _buffer = new byte[paddedSize];

        Array.Copy(plainData, _buffer, plainData.Length);
        Array.Clear(plainData);

        // 1. Encrypt in RAM
        if (!CryptProtectMemory(_buffer, _buffer.Length, 0))
            throw new CryptographicException("Memory protection failed.");

        // 2. Lock in RAM
        _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);

        if (!VirtualLock(_handle.AddrOfPinnedObject(), (UIntPtr)_buffer.Length))
        {
            NotificationService.Add("VirtualLock failed. Data is encrypted but might be subject to page swapping.",
                                    NotificationType.Warning);
        }
    }

    public void AccessData(Action<byte[]> action)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        byte[] actualData = new byte[_dataLength];
        bool Encrypted = false;
        if (!CryptUnprotectMemory(_buffer, _buffer.Length, 0))
            throw new CryptographicException("Memory unprotection failed.");
        try
        {
            Array.Copy(_buffer, actualData, _dataLength);
            Encrypted = CryptProtectMemory(_buffer, _buffer.Length, 0);
            action(actualData);
            Array.Clear(actualData);
        }
        finally
        {
            if (!Encrypted)
                if (!CryptProtectMemory(_buffer, _buffer.Length, 0))
                    NotificationService.Add("Memory protection failed.", NotificationType.Error);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (_handle.IsAllocated)
        {
            VirtualUnlock(_handle.AddrOfPinnedObject(), (UIntPtr)_buffer.Length);
            _handle.Free();
        }

        Array.Clear(_buffer);
        _isDisposed = true;
    }
}