using System.Windows;
using System.Windows.Threading;

namespace TextHelper.Services;

/// <summary>
/// 剪贴板服务 — 读取选中的文本
/// </summary>
public class ClipboardService
{
    /// <summary>
    /// 从剪贴板读取文本
    /// 需要在 STA 线程中调用（WPF 自动满足）
    /// </summary>
    public string? GetText()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                return System.Windows.Clipboard.GetText();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 尝试多次读取剪贴板（有时复制后剪贴板还没更新）
    /// </summary>
    public string? WaitForText(int timeoutMs = 500)
    {
        string? result = null;
        var timer = System.Diagnostics.Stopwatch.StartNew();

        while (timer.ElapsedMilliseconds < timeoutMs)
        {
            result = GetText();
            if (!string.IsNullOrWhiteSpace(result))
                break;
            Thread.Sleep(50);
        }

        return result;
    }
}
