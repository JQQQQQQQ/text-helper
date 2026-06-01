using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WpfMessageBox = System.Windows.MessageBox;

namespace TextHelper;

public partial class SettingsWindow : Window
{
    public SettingsWindow(IConfiguration? config)
    {
        InitializeComponent();
        LoadRateOptions();
        LoadConfigToUI(config);
        UpdateDeepSeekVisibility();
    }

    private void LoadRateOptions()
    {
        for (int i = -10; i <= 10; i++)
        {
            RateComboBox.Items.Add(i);
        }
    }

    private void LoadConfigToUI(IConfiguration? config)
    {
        var provider = (config?["Translation:Provider"] ?? "deepseek").ToLowerInvariant();
        ProviderComboBox.SelectedIndex = provider == "google" ? 1 : 0;

        ApiKeyPasswordBox.Password = config?["DeepSeek:ApiKey"] ?? string.Empty;
        ModelTextBox.Text = config?["DeepSeek:Model"] ?? "deepseek-chat";

        var autoReadVal = config?["TTS:AutoRead"];
        AutoReadCheckBox.IsChecked = autoReadVal is null || !bool.TryParse(autoReadVal, out var autoRead) || autoRead;

        var rateVal = config?["TTS:Rate"];
        if (int.TryParse(rateVal, out var rate) && rate >= -10 && rate <= 10)
        {
            RateComboBox.SelectedItem = rate;
        }
        else
        {
            RateComboBox.SelectedItem = 0;
        }
    }

    private bool IsDeepSeekSelected()
    {
        return (ProviderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "DeepSeek";
    }

    private void UpdateDeepSeekVisibility()
    {
        DeepSeekPanel.Visibility = IsDeepSeekSelected() ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDeepSeekVisibility();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(baseDir, "appsettings.json");

            if (!File.Exists(configPath))
            {
                WpfMessageBox.Show($"未找到配置文件: {configPath}", "TextHelper", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var json = File.ReadAllText(configPath);
            var root = JObject.Parse(json);

            var provider = IsDeepSeekSelected() ? "deepseek" : "google";
            root["Translation"] ??= new JObject();
            root["Translation"]!["Provider"] = provider;

            root["DeepSeek"] ??= new JObject();
            root["DeepSeek"]!["ApiKey"] = ApiKeyPasswordBox.Password;
            root["DeepSeek"]!["Model"] = string.IsNullOrWhiteSpace(ModelTextBox.Text) ? "deepseek-chat" : ModelTextBox.Text.Trim();

            root["TTS"] ??= new JObject();
            root["TTS"]!["AutoRead"] = AutoReadCheckBox.IsChecked ?? true;
            root["TTS"]!["Rate"] = RateComboBox.SelectedItem is int rate ? rate : 0;

            File.WriteAllText(configPath, root.ToString(Formatting.Indented));

            WpfMessageBox.Show("设置已保存", "TextHelper", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"保存失败: {ex.Message}", "TextHelper", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
