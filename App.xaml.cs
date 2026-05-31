using System.Windows;
using System.Windows.Interop;
using TextHelper.Services;
using Microsoft.Extensions.Configuration;
using Forms = System.Windows.Forms;

namespace TextHelper;

public partial class App : System.Windows.Application
{
    private HotkeyService? _hotkeyService;
    private ClipboardService? _clipboardService;
    private TranslationService? _translationService;
    private TtsService? _ttsService;
    private Forms.NotifyIcon? _trayIcon;
    private HwndSource? _hwndSource;
    private PopupWindow? _popupWindow;
    private IConfiguration? _config;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. 加载配置
        LoadConfig();

        // 2. 初始化服务
        _clipboardService = new ClipboardService();
        _ttsService = new TtsService();

        var apiKey = _config?["DeepSeek:ApiKey"] ?? "";
        var model = _config?["DeepSeek:Model"] ?? "deepseek-chat";
        _translationService = new TranslationService(apiKey, model);

        // 3. 创建托盘图标
        CreateTrayIcon();

        // 4. 创建隐藏窗口并注册热键
        CreateHiddenWindow();

        // 5. 注册全局热键
        RegisterHotkey();
    }

    private void LoadConfig()
    {
        try
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            _config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show($"配置文件加载失败: {ex.Message}\n请确保 appsettings.json 存在", "TextHelper",
                Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Warning);
        }
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,  // 可以用自定义图标替换
            Text = "TextHelper - 划词翻译",
            Visible = true
        };

        // 右键菜单
        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("显示", null, (s, e) => ShowPopup("请先复制文本，然后按快捷键"));
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("退出", null, (s, e) => ExitApp());

        _trayIcon.ContextMenuStrip = contextMenu;

        // 双击托盘图标显示
        _trayIcon.MouseDoubleClick += (s, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                ShowPopup("TextHelper 正在运行\n快捷键: Ctrl+Alt+C");
        };
    }

    private void CreateHiddenWindow()
    {
        // 创建一个隐藏窗口来接收 Win32 消息（热键）
        var window = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Visibility = Visibility.Hidden
        };
        window.Show();

        _hwndSource = PresentationSource.FromVisual(window) as HwndSource;
        if (_hwndSource != null)
        {
            _hotkeyService = new HotkeyService(_hwndSource.Handle);
            _hwndSource.AddHook(WndProc);
        }
    }

    private void RegisterHotkey()
    {
        var modifiers = _config?["Hotkey:Modifiers"] ?? "Ctrl+Alt";
        var key = _config?["Hotkey:Key"] ?? "C";

        if (_hotkeyService != null)
        {
            var keyEnum = (System.Windows.Input.Key)Enum.Parse(
                typeof(System.Windows.Input.Key), key, true);

            if (!_hotkeyService.Register(modifiers, keyEnum))
            {
                _trayIcon?.ShowBalloonTip(3000, "TextHelper",
                    $"快捷键 {modifiers}+{key} 注册失败，可能被其他程序占用",
                    Forms.ToolTipIcon.Warning);
            }
            else
            {
                _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        _hotkeyService?.ProcessMessage(hwnd, msg, wParam, lParam);
        return IntPtr.Zero;
    }

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // 延迟一小段时间等待剪贴板更新
        await Task.Delay(100);

        var text = _clipboardService?.WaitForText();
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowPopup("⚠️ 请先选中文本并复制 (Ctrl+C)");
            return;
        }

        if (text.Length > 500)
        {
            text = text[..500] + "...";
        }

        // 关闭之前的弹出窗口
        ClosePopup();

        // 显示"翻译中..."
        ShowPopup($"📖 {text}\n\n⏳ 翻译中...");

        // 调用翻译
        var result = await _translationService!.TranslateAsync(text);

        // 显示结果
        ClosePopup();
        ShowPopup(text, result);

        // 自动朗读
        var autoReadVal = _config?.GetSection("TTS:AutoRead")?.Value;
        bool autoRead = autoReadVal is null || !bool.TryParse(autoReadVal, out var parsed) || parsed;
        if (autoRead && result != null && !string.IsNullOrWhiteSpace(result.Translation))
        {
            _ttsService?.Speak(text);
        }
    }

    private void ShowPopup(string statusMessage)
    {
        _popupWindow?.Close();
        _popupWindow = new PopupWindow(statusMessage);
        _popupWindow.Closed += (s, e) => _popupWindow = null;
        _popupWindow.Show();
    }

    private void ShowPopup(string originalText, TranslationResult? result)
    {
        _popupWindow?.Close();
        _popupWindow = new PopupWindow(originalText, result, _ttsService);
        _popupWindow.Closed += (s, e) => _popupWindow = null;
        _popupWindow.Show();
    }

    private void ClosePopup()
    {
        _popupWindow?.Close();
        _popupWindow = null;
    }

    private void ExitApp()
    {
        _hotkeyService?.Dispose();
        _ttsService?.Dispose();
        _trayIcon?.Dispose();
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _ttsService?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
