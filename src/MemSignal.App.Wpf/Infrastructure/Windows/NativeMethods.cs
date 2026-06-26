using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MemSignal.Infrastructure.Windows;

internal static partial class NativeMethods
{
    public const int ErrorSuccess = 0;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmCornerPreferenceRoundSmall = 3;

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetPerformanceInfo(out PerformanceInformation performanceInformation, int size);

    [LibraryImport("pdh.dll", EntryPoint = "PdhOpenQueryW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int PdhOpenQuery(string? dataSource, IntPtr userData, out IntPtr query);

    [LibraryImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int PdhAddEnglishCounter(IntPtr query, string fullCounterPath, IntPtr userData, out IntPtr counter);

    [LibraryImport("pdh.dll")]
    public static partial int PdhCollectQueryData(IntPtr query);

    [LibraryImport("pdh.dll")]
    public static partial int PdhGetFormattedCounterValue(
        IntPtr counter,
        uint format,
        out uint type,
        out PdhFormattedCounterValue value);

    [LibraryImport("pdh.dll")]
    public static partial int PdhCloseQuery(IntPtr query);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    public static void ApplySmallRoundedCorners(IntPtr windowHandle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var preference = DwmCornerPreferenceRoundSmall;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DwmWindowCornerPreference,
            ref preference,
            sizeof(int));
    }

    public static PerformanceInformation GetPerformanceInformation()
    {
        var info = new PerformanceInformation
        {
            Size = Marshal.SizeOf<PerformanceInformation>()
        };

        if (!GetPerformanceInfo(out info, info.Size))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to read Windows memory performance information.");
        }

        return info;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerformanceInformation
    {
        public int Size;
        public UIntPtr CommitTotal;
        public UIntPtr CommitLimit;
        public UIntPtr CommitPeak;
        public UIntPtr PhysicalTotal;
        public UIntPtr PhysicalAvailable;
        public UIntPtr SystemCache;
        public UIntPtr KernelTotal;
        public UIntPtr KernelPaged;
        public UIntPtr KernelNonpaged;
        public UIntPtr PageSize;
        public int HandleCount;
        public int ProcessCount;
        public int ThreadCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PdhFormattedCounterValue
    {
        public uint CStatus;
        public double DoubleValue;
    }
}
