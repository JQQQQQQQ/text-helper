using System.Windows;
using System.Windows.Input;
using TextHelper.Services;

namespace TextHelper;

public partial class PopupWindow : Window
{
    private readonly TtsService? _ttsService;

    /// <summary>
    /// 状态消息模式（只显示文字）
    /// </summary>
    public PopupWindow(string statusMessage)
    {
        InitializeComponent();
        OriginalText.Text = "";
        TranslationText.Text = statusMessage;
        ExplanationText.Text = "";
        ExamplesList.Visibility = Visibility.Collapsed;
        SpeakButton.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// 翻译结果模式
    /// </summary>
    public PopupWindow(string originalText, TranslationResult? result, TtsService? ttsService)
    {
        InitializeComponent();

        _ttsService = ttsService;
        OriginalText.Text = originalText;

        if (result != null)
        {
            TranslationText.Text = result.Translation;
            ExplanationText.Text = result.Explanation;
            ExamplesList.ItemsSource = result.Examples;
        }
    }

    /// <summary>
    /// 点击窗口外自动关闭
    /// </summary>
    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SpeakButton_Click(object sender, RoutedEventArgs e)
    {
        var text = OriginalText.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            _ttsService?.Speak(text);
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = TranslationText.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                System.Windows.Clipboard.SetText(text);
                CopyButton.Content = "✅ 已复制";
            }
        }
        catch { }
    }
}
