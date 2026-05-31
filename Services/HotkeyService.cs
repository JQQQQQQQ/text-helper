using System.Runtime.InteropServices;
using System.Windows.Input;

namespace TextHelper.Services;

/// <summary>
/// 全局快捷键服务
/// 使用 Win32 API RegisterHotKey 注册全局热键
/// </summary>
public class HotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private readonly IntPtr _hWnd;
    private bool _disposed;

    /// <summary>
    /// 热键被按下时触发
    /// </summary>
    public event EventHandler? HotkeyPressed;

    public HotkeyService(IntPtr windowHandle)
    {
        _hWnd = windowHandle;
    }

    /// <summary>
    /// 注册全局热键
    /// </summary>
    public bool Register(string modifiers, Key key)
    {
        uint mod = 0;
        if (modifiers.Contains("Ctrl")) mod |= 0x0002;  // MOD_CONTROL
        if (modifiers.Contains("Alt")) mod |= 0x0001;    // MOD_ALT
        if (modifiers.Contains("Shift")) mod |= 0x0004;  // MOD_SHIFT
        if (modifiers.Contains("Win")) mod |= 0x0008;    // MOD_WIN

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return RegisterHotKey(_hWnd, HOTKEY_ID, mod, vk);
    }

    /// <summary>
    /// 处理窗口消息，当收到 WM_HOTKEY 时触发事件
    /// </summary>
    public void ProcessMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterHotKey(_hWnd, HOTKEY_ID);
            _disposed = true;
        }
    }
}
