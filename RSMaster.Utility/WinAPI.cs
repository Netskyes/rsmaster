using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace RSMaster.Utility
{
    public static class WinAPI
    {
        [DllImport("user32.dll")]
        public static extern int SetWindowText(IntPtr hWnd, string text);
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref WinAPI.SECURITY_ATTRIBUTES lpProcessAttributes, ref WinAPI.SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref WinAPI.STARTUPINFO lpStartupInfo, out WinAPI.PROCESS_INFORMATION lpProcessInformation);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref WinAPI.STARTUPINFO lpStartupInfo, out WinAPI.PROCESS_INFORMATION lpProcessInformation);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, WinAPI.ShowWindowCommands nCmdShow);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetLongPathName(string ShortPath, StringBuilder sb, int buffer);
        [DllImport("kernel32.dll")]
        private static extern uint GetShortPathName(string longpath, StringBuilder sb, int buffer);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(uint hHandle, uint dwMilliseconds);
        public static string GetWindowsPhysicalPath(string path)
        {
            StringBuilder stringBuilder = new StringBuilder(255);
            WinAPI.GetShortPathName(path, stringBuilder, stringBuilder.Capacity);
            path = stringBuilder.ToString();
            uint longPathName = WinAPI.GetLongPathName(path, stringBuilder, stringBuilder.Capacity);
            if (longPathName > 0u && (ulong)longPathName < (ulong)((long)stringBuilder.Capacity))
            {
                stringBuilder[0] = char.ToUpper(stringBuilder[0]);
                return stringBuilder.ToString(0, (int)longPathName);
            }
            if (longPathName > 0u)
            {
                stringBuilder = new StringBuilder((int)longPathName);
                longPathName = WinAPI.GetLongPathName(path, stringBuilder, stringBuilder.Capacity);
                stringBuilder[0] = char.ToUpper(stringBuilder[0]);
                return stringBuilder.ToString(0, (int)longPathName);
            }
            return null;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetFinalPathNameByHandle(IntPtr handle, [In] [Out] StringBuilder path, int bufLen, int flags);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, WinAPI.LowLevelKeyboardProcDelegate lpfn, IntPtr hMod, int dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(IntPtr lpModuleName);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, int dwProcessId);
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();
        [DllImport("kernel32.dll")]
        public static extern bool ReleaseMutex(IntPtr hMutex);
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(WinAPI.EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindowLong")]
        public static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
        public static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFileMapping(uint hFile, uint lpFileMappingAttributes, uint flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFileMapping(uint hFile, IntPtr lpFileMappingAttributes, WinAPI.FileMapProtection flProtect, int dwMaximumSizeHigh, int dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr Destination, byte[] Source, int Length);
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(byte[] Destination, IntPtr Source, int Length);
        [DllImport("Gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        public static int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return (int)WinAPI.GetWindowLong32(hWnd, nIndex);
            }
            return (int)((long)WinAPI.GetWindowLongPtr64(hWnd, nIndex));
        }
        public static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            if (IntPtr.Size == 4)
            {
                return (int)WinAPI.SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
            }
            return (int)((long)WinAPI.SetWindowLongPtr64(hWnd, nIndex, dwNewLong));
        }
        public static ushort Hiword(IntPtr dwValue)
        {
            return (ushort)((long)dwValue >> 16 & 65535L);
        }
        public static int GET_WHEEL_DELTA_WPARAM(IntPtr wParam)
        {
            return (int)((short)WinAPI.Hiword(wParam));
        }
        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetFileVersion(string path, StringBuilder buffer, int buflen, out int written);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(WinAPI.ProcessAccessFlags dwDesiredAccess, int bInheritHandle, uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(WinAPI.SnapshotFlags dwFlags, uint th32ProcessID);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool Process32First([In] IntPtr hSnapshot, ref WinAPI.PROCESSENTRY32 lppe);
        [DllImport("winmm.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint waveOutGetDevCaps(uint hwo, ref WinAPI.WAVEOUTCAPS pwoc, uint cbwoc);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool Process32Next([In] IntPtr hSnapshot, ref WinAPI.PROCESSENTRY32 lppe);
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveOutGetNumDevs();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetRawInputDeviceList(IntPtr RawInputDeviceList, ref int NumDevices, int Size);
        [DllImport("user32.dll")]
        public static extern uint GetRawInputDeviceInfo(IntPtr deviceHandle, int command, ref WinAPI.DeviceInfo data, ref int dataSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "CreateFileMapping", SetLastError = true)]
        private static extern IntPtr CreateFileMapping64(long hFile, IntPtr lpFileMappingAttributes, WinAPI.FileMapProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "CreateFileMapping", SetLastError = true)]
        private static extern IntPtr CreateFileMapping32(int hFile, IntPtr lpFileMappingAttributes, WinAPI.FileMapProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        public static IntPtr CreateFileMapping(long hFile, IntPtr lpFileMappingAttributes, WinAPI.FileMapProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName)
        {
            if (IntPtr.Size == 8)
            {
                return WinAPI.CreateFileMapping64(hFile, lpFileMappingAttributes, flProtect, dwMaximumSizeHigh, dwMaximumSizeLow, lpName);
            }
            return WinAPI.CreateFileMapping32((int)hFile, lpFileMappingAttributes, flProtect, dwMaximumSizeHigh, dwMaximumSizeLow, lpName);
        }
        [DllImport("kernel32.dll", EntryPoint = "MapViewOfFile", SetLastError = true)]
        private static extern IntPtr MapViewOfFile64(IntPtr hFileMappingObject, WinAPI.FileMapAccess dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, long dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", EntryPoint = "MapViewOfFile", SetLastError = true)]
        private static extern IntPtr MapViewOfFile32(IntPtr hFileMappingObject, WinAPI.FileMapAccess dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, int dwNumberOfBytesToMap);
        public static IntPtr MapViewOfFile(IntPtr hFileMappingObject, WinAPI.FileMapAccess dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, long dwNumberOfBytesToMap)
        {
            if (IntPtr.Size == 8)
            {
                return WinAPI.MapViewOfFile64(hFileMappingObject, dwDesiredAccess, dwFileOffsetHigh, dwFileOffsetLow, dwNumberOfBytesToMap);
            }
            return WinAPI.MapViewOfFile32(hFileMappingObject, dwDesiredAccess, dwFileOffsetHigh, dwFileOffsetLow, (int)dwNumberOfBytesToMap);
        }
        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pwszBuff, int cchBuff, uint wFlags);
        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);
        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, WinAPI.MapType uMapType);
        [SecurityCritical]
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern bool RtlGetVersion(ref WinAPI.OSVERSIONINFOEX versionInfo);
        public const int GwlStyle = -16;
        public const int WsExTransparent = 32;
        public const int WsExLayered = 524288;
        public const int WsBorder = 8388608;
        public const int WmActivate = 6;
        public const int LwaAlpha = 2;
        public const int WsExWindowedge = 256;
        public const int WsExClientedge = 512;
        public const int WsExNoactivate = 134217728;
        public const int WmMousewheel = 522;
        public const int WmHscroll = 276;
        public const int WmVscroll = 277;
        public const int LvmFirst = 4096;
        public const int LvmScroll = 4116;
        public const int WsHscroll = 1048576;
        public const int WsVscroll = 2097152;
        public const int WmMousemove = 512;
        public const int WmSetfocus = 7;
        public const int WsCaption = 12582912;
        public const int WsThickframe = 262144;
        public const int WsExAppwindow = 262144;
        public const int WsOverlapped = 0;
        public const int WsPopup = -2147483648;
        public const int WhKeyboardLl = 13;
        public const int WmKeydown = 256;
        public const int WmKeyup = 257;
        public const int WmSyskeydown = 260;
        public const int WmSyskeyup = 261;
        private const uint STANDARD_RIGHTS_REQUIRED = 983040u;
        private const uint SECTION_QUERY = 1u;
        private const uint SECTION_MAP_WRITE = 2u;
        private const uint SECTION_MAP_READ = 4u;
        private const uint SECTION_MAP_EXECUTE = 8u;
        private const uint SECTION_EXTEND_SIZE = 16u;
        private const uint SECTION_ALL_ACCESS = 983071u;
        private const uint FILE_MAP_ALL_ACCESS = 983071u;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESSENTRY32
        {
            private const int MAX_PATH = 260;
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }
        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 1u,
            Process = 2u,
            Thread = 4u,
            Module = 8u,
            Module32 = 16u,
            All = 15u,
            Inherit = 2147483648u,
            NoHeaps = 1073741824u
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
        }
        public enum RawInputDeviceType : uint
        {
            MOUSE,
            KEYBOARD,
            HID
        }
        public struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public WinAPI.RawInputDeviceType Type;
        }
        public enum ShowCommands
        {
            SW_HIDE,
            SW_SHOWNORMAL,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED,
            SW_SHOWMAXIMIZED,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE,
            SW_SHOW,
            SW_MINIMIZE,
            SW_SHOWMINNOACTIVE,
            SW_SHOWNA,
            SW_RESTORE,
            SW_SHOWDEFAULT,
            SW_FORCEMINIMIZE,
            SW_MAX = 11
        }
        public enum ShowWindowCommands
        {
            Hide,
            Normal,
            ShowMinimized,
            Maximize,
            ShowMaximized = 3,
            ShowNoActivate,
            Show,
            Minimize,
            ShowMinNoActive,
            ShowNA,
            Restore,
            ShowDefault,
            ForceMinimize
        }
        [Flags]
        public enum ShellExecuteMaskFlags : uint
        {
            SEE_MASK_DEFAULT = 0u,
            SEE_MASK_CLASSNAME = 1u,
            SEE_MASK_CLASSKEY = 3u,
            SEE_MASK_IDLIST = 4u,
            SEE_MASK_INVOKEIDLIST = 12u,
            SEE_MASK_HOTKEY = 32u,
            SEE_MASK_NOCLOSEPROCESS = 64u,
            SEE_MASK_CONNECTNETDRV = 128u,
            SEE_MASK_NOASYNC = 256u,
            SEE_MASK_FLAG_DDEWAIT = 256u,
            SEE_MASK_DOENVSUBST = 512u,
            SEE_MASK_FLAG_NO_UI = 1024u,
            SEE_MASK_UNICODE = 16384u,
            SEE_MASK_NO_CONSOLE = 32768u,
            SEE_MASK_ASYNCOK = 1048576u,
            SEE_MASK_HMONITOR = 2097152u,
            SEE_MASK_NOZONECHECKS = 8388608u,
            SEE_MASK_NOQUERYCLASSSTORE = 16777216u,
            SEE_MASK_WAITFORINPUTIDLE = 33554432u,
            SEE_MASK_FLAG_LOG_USAGE = 67108864u
        }
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 2035711u,
            Terminate = 1u,
            CreateThread = 2u,
            VirtualMemoryOperation = 8u,
            VirtualMemoryRead = 16u,
            VirtualMemoryWrite = 32u,
            DuplicateHandle = 64u,
            CreateProcess = 128u,
            SetQuota = 256u,
            SetInformation = 512u,
            QueryInformation = 1024u,
            QueryLimitedInformation = 4096u,
            Synchronize = 1048576u
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct DeviceInfo
        {
            [FieldOffset(0)]
            public int Size;
            [FieldOffset(4)]
            public int Type;
            [FieldOffset(8)]
            public WinAPI.DeviceInfoMouse MouseInfo;
            [FieldOffset(8)]
            public WinAPI.DeviceInfoKeyboard KeyboardInfo;
            [FieldOffset(8)]
            public WinAPI.DeviceInfoHID HIDInfo;
        }
        public struct DeviceInfoMouse
        {
            public uint ID;
            public uint NumberOfButtons;
            public uint SampleRate;
        }
        public struct DeviceInfoKeyboard
        {
            public uint Type;
            public uint SubType;
            public uint KeyboardMode;
            public uint NumberOfFunctionKeys;
            public uint NumberOfIndicators;
            public uint NumberOfKeysTotal;
        }
        public struct DeviceInfoHID
        {
            public uint VendorID;
            public uint ProductID;
            public uint VersionNumber;
            public ushort UsagePage;
            public ushort Usage;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        // (Invoke) Token: 0x060000F1 RID: 241
        public delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, IntPtr lParam);
        [Flags]
        public enum FileMapAccess : uint
        {
            FileMapCopy = 1u,
            FileMapWrite = 2u,
            FileMapRead = 4u,
            FileMapAllAccess = 31u,
            FileMapExecute = 32u
        }
        [Flags]
        public enum FileMapProtection : uint
        {
            PageReadonly = 2u,
            PageReadWrite = 4u,
            PageWriteCopy = 8u,
            PageExecuteRead = 32u,
            PageExecuteReadWrite = 64u,
            SectionCommit = 134217728u,
            SectionImage = 16777216u,
            SectionNoCache = 268435456u,
            SectionReserve = 67108864u
        }
        // (Invoke) Token: 0x060000F5 RID: 245
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC,
            MAPVK_VSC_TO_VK,
            MAPVK_VK_TO_CHAR,
            MAPVK_VSC_TO_VK_EX
        }
        public struct OSVERSIONINFOEX
        {
            public int OSVersionInfoSize;
            public int MajorVersion;
            public int MinorVersion;
            public int BuildNumber;
            public int PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
            public ushort ServicePackMajor;
            public ushort ServicePackMinor;
            public short SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
    }
}
