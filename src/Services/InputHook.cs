using System.Runtime.InteropServices;
using AdvancedInputOverlay.Models;

namespace AdvancedInputOverlay.Services;

/// <summary>
/// Global low-level keyboard + mouse hook. Runs its message loop on a dedicated
/// background thread (WH_KEYBOARD_LL / WH_MOUSE_LL require the installing thread to
/// pump messages). Hook callbacks must return quickly; we only translate the event
/// to a key name and raise <see cref="KeyChanged"/>. Subscribers are responsible
/// for marshalling to the UI thread.
/// </summary>
public sealed class InputHook : IDisposable
{
    // ---- Win32 ----
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private const int WM_QUIT = 0x0012;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_XBUTTONUP = 0x020C;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public int ptX;
        public int ptY;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    // ---- State ----
    /// <summary>Fires on the hook background thread. Subscribers must dispatch to UI thread themselves.</summary>
    public event Action<string, bool>? KeyChanged;

    private Thread? _thread;
    private uint _threadId;
    private IntPtr _kbHook;
    private IntPtr _mouseHook;
    private HookProc? _kbProc;       // keep delegate alive against GC
    private HookProc? _mouseProc;
    private readonly ManualResetEventSlim _started = new(false);
    private bool _disposed;

    public bool IsRunning => _thread is { IsAlive: true };

    public void Start()
    {
        if (IsRunning) return;
        _thread = new Thread(ThreadProc)
        {
            Name = "AdvancedInputOverlay.InputHook",
            IsBackground = true,
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _started.Wait(TimeSpan.FromSeconds(2));
    }

    public void Stop()
    {
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _threadId = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _started.Dispose();
    }

    private void ThreadProc()
    {
        _threadId = GetCurrentThreadId();
        _kbProc = KeyboardCallback;
        _mouseProc = MouseCallback;
        var hMod = GetModuleHandle(null);

        _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, hMod, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);

        _started.Set();

        // Standard message loop until WM_QUIT
        while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        if (_kbHook != IntPtr.Zero) UnhookWindowsHookEx(_kbHook);
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        _kbHook = IntPtr.Zero;
        _mouseHook = IntPtr.Zero;
    }

    private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam.ToInt64();
            bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;
            if (isDown || isUp)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if (KeyMap.TryGetName((int)data.vkCode, out var name))
                {
                    try { KeyChanged?.Invoke(name, isDown); }
                    catch { /* never let exceptions escape the hook callback */ }
                }
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam.ToInt64();
            string? key = null;
            bool isDown = false;
            switch (msg)
            {
                case WM_LBUTTONDOWN: key = "MouseLeft"; isDown = true; break;
                case WM_LBUTTONUP:   key = "MouseLeft"; isDown = false; break;
                case WM_RBUTTONDOWN: key = "MouseRight"; isDown = true; break;
                case WM_RBUTTONUP:   key = "MouseRight"; isDown = false; break;
                case WM_MBUTTONDOWN: key = "MouseMiddle"; isDown = true; break;
                case WM_MBUTTONUP:   key = "MouseMiddle"; isDown = false; break;
                case WM_XBUTTONDOWN:
                case WM_XBUTTONUP:
                {
                    var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    int x = (int)((data.mouseData >> 16) & 0xFFFF);
                    key = x == 1 ? "MouseSide1" : "MouseSide2";
                    isDown = msg == WM_XBUTTONDOWN;
                    break;
                }
            }
            if (key != null)
            {
                try { KeyChanged?.Invoke(key, isDown); }
                catch { /* never let exceptions escape the hook callback */ }
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }
}
