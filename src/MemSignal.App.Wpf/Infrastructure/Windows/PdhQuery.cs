using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemSignal.Infrastructure.Windows;

internal sealed class PdhQuery : IDisposable
{
    private readonly IntPtr _handle;
    private bool _disposed;

    private PdhQuery(IntPtr handle)
    {
        _handle = handle;
    }

    public static PdhQuery Open()
    {
        var status = NativeMethods.PdhOpenQuery(null, IntPtr.Zero, out var handle);
        if (status != NativeMethods.ErrorSuccess)
        {
            throw new Win32Exception(status, "Unable to open a PDH query.");
        }

        return new PdhQuery(handle);
    }

    public PdhCounter AddRequiredEnglishCounter(string path)
    {
        var counter = TryAddEnglishCounter(path);
        return counter ?? throw new InvalidOperationException($"Required performance counter is unavailable: {path}");
    }

    public PdhCounter? TryAddEnglishCounter(string path)
    {
        var status = NativeMethods.PdhAddEnglishCounter(_handle, path, IntPtr.Zero, out var counterHandle);
        return status == NativeMethods.ErrorSuccess
            ? new PdhCounter(counterHandle, path)
            : null;
    }

    public void Collect()
    {
        var status = NativeMethods.PdhCollectQueryData(_handle);
        if (status != NativeMethods.ErrorSuccess)
        {
            throw new Win32Exception(status, "Unable to collect PDH query data.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.PdhCloseQuery(_handle);
        _disposed = true;
    }
}
