using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TphdCemuTrainer.Memory;

public sealed record ProcessAttachDiagnostics(TimeSpan ProcessDiscoveryTime, TimeSpan HandleOpenTime);

public sealed class ProcessMemory : IDisposable
{
    private const string CemuProcessName = "Cemu";
    private const uint ProcessQueryInformation = 0x0400;
    private const uint ProcessVmOperation = 0x0008;
    private const uint ProcessVmRead = 0x0010;
    private const uint ProcessVmWrite = 0x0020;
    private const uint DesiredAccess = ProcessQueryInformation | ProcessVmOperation | ProcessVmRead | ProcessVmWrite;

    private readonly Process _process;
    private IntPtr _handle;

    private ProcessMemory(Process process, IntPtr handle)
    {
        _process = process;
        _handle = handle;
    }

    public int ProcessId => _process.Id;

    public string ProcessName => $"{_process.ProcessName}.exe";

    public bool HasExited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }

    public static bool TryAttachToCemu(out ProcessMemory? memory, out string error)
    {
        return TryAttachToCemu(out memory, out error, out _);
    }

    public static bool TryAttachToCemu(
        out ProcessMemory? memory,
        out string error,
        out ProcessAttachDiagnostics diagnostics)
    {
        memory = null;
        error = string.Empty;
        diagnostics = new ProcessAttachDiagnostics(TimeSpan.Zero, TimeSpan.Zero);

        var discoveryWatch = Stopwatch.StartNew();
        var process = Process.GetProcessesByName(CemuProcessName).FirstOrDefault();
        discoveryWatch.Stop();
        if (process is null)
        {
            diagnostics = diagnostics with { ProcessDiscoveryTime = discoveryWatch.Elapsed };
            error = "Cemu.exe is not running.";
            return false;
        }

        var handleWatch = Stopwatch.StartNew();
        var handle = OpenProcess(DesiredAccess, false, process.Id);
        handleWatch.Stop();
        diagnostics = new ProcessAttachDiagnostics(discoveryWatch.Elapsed, handleWatch.Elapsed);

        if (handle == IntPtr.Zero)
        {
            error = $"Could not open Cemu.exe: {GetLastWin32ErrorMessage()}";
            process.Dispose();
            return false;
        }

        memory = new ProcessMemory(process, handle);
        return true;
    }

    public bool TryReadBytes(ulong address, int length, out byte[] bytes, out int bytesRead)
    {
        bytes = new byte[length];
        bytesRead = 0;

        if (_handle == IntPtr.Zero || HasExited)
        {
            return false;
        }

        var success = ReadProcessMemory(_handle, ToIntPtr(address), bytes, length, out var nativeBytesRead);
        bytesRead = nativeBytesRead.ToInt32();

        if (bytesRead > 0 && bytesRead < bytes.Length)
        {
            Array.Resize(ref bytes, bytesRead);
        }

        return success && bytesRead > 0;
    }

    public bool TryWriteBytes(ulong address, byte[] bytes, out string error)
    {
        error = string.Empty;

        if (_handle == IntPtr.Zero || HasExited)
        {
            error = "Cemu.exe is no longer available.";
            return false;
        }

        var success = WriteProcessMemory(_handle, ToIntPtr(address), bytes, bytes.Length, out var nativeBytesWritten);
        var bytesWritten = nativeBytesWritten.ToInt32();

        if (!success || bytesWritten != bytes.Length)
        {
            error = $"Could not write {bytes.Length} byte(s) at 0x{address:X}: {GetLastWin32ErrorMessage()}";
            return false;
        }

        return true;
    }

    internal bool TryQueryRegion(ulong address, out MemoryRegion region)
    {
        region = default;

        if (_handle == IntPtr.Zero || HasExited)
        {
            return false;
        }

        var result = VirtualQueryEx(
            _handle,
            ToIntPtr(address),
            out var info,
            (UIntPtr)Marshal.SizeOf<MemoryBasicInformation>());

        if (result == UIntPtr.Zero)
        {
            return false;
        }

        region = new MemoryRegion(
            ToUInt64(info.BaseAddress),
            info.RegionSize.ToUInt64(),
            info.State,
            info.Protect,
            info.Type);

        return true;
    }

    internal static (ulong Minimum, ulong Maximum) GetSystemAddressRange()
    {
        GetNativeSystemInfo(out var info);
        return (ToUInt64(info.MinimumApplicationAddress), ToUInt64(info.MaximumApplicationAddress));
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }

        _process.Dispose();
    }

    private static IntPtr ToIntPtr(ulong address) => new(unchecked((long)address));

    private static ulong ToUInt64(IntPtr pointer) => unchecked((ulong)pointer.ToInt64());

    private static string GetLastWin32ErrorMessage()
    {
        var error = Marshal.GetLastWin32Error();
        return error == 0 ? "Unknown Win32 error." : new Win32Exception(error).Message;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int nSize,
        out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr VirtualQueryEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        out MemoryBasicInformation lpBuffer,
        UIntPtr dwLength);

    [DllImport("kernel32.dll")]
    private static extern void GetNativeSystemInfo(out SystemInfo lpSystemInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public UIntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemInfo
    {
        public ushort ProcessorArchitecture;
        public ushort Reserved;
        public uint PageSize;
        public IntPtr MinimumApplicationAddress;
        public IntPtr MaximumApplicationAddress;
        public IntPtr ActiveProcessorMask;
        public uint NumberOfProcessors;
        public uint ProcessorType;
        public uint AllocationGranularity;
        public ushort ProcessorLevel;
        public ushort ProcessorRevision;
    }
}
