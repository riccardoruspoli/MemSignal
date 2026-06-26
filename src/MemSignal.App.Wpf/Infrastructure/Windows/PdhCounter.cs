using System.ComponentModel;

namespace MemSignal.Infrastructure.Windows;

internal sealed class PdhCounter
{
    private const uint PdhFmtDouble = 0x00000200;
    private readonly IntPtr _handle;
    private readonly string _path;

    public PdhCounter(IntPtr handle, string path)
    {
        _handle = handle;
        _path = path;
    }

    public double GetDoubleValue()
    {
        var status = NativeMethods.PdhGetFormattedCounterValue(
            _handle,
            PdhFmtDouble,
            out _,
            out var value);

        if (status != NativeMethods.ErrorSuccess)
        {
            throw new Win32Exception(status, $"Unable to read performance counter: {_path}");
        }

        if (value.CStatus != NativeMethods.ErrorSuccess)
        {
            throw new Win32Exception((int)value.CStatus, $"Performance counter returned invalid data: {_path}");
        }

        return Math.Max(0, value.DoubleValue);
    }
}
