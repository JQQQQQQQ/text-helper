using System.Speech.Synthesis;

namespace TextHelper.Services;

/// <summary>
/// 语音朗读服务 — 使用 Windows TTS 朗读英文
/// </summary>
public class TtsService : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer;

    public TtsService()
    {
        _synthesizer = new SpeechSynthesizer();

        // 选择英语语音（如果有的话）
        SelectEnglishVoice();
    }

    private void SelectEnglishVoice()
    {
        try
        {
            var installed = _synthesizer.GetInstalledVoices();
            foreach (var voice in installed)
            {
                if (voice.VoiceInfo?.Culture.Name.StartsWith("en") == true ||
                    voice.VoiceInfo?.Name.Contains("English") == true ||
                    voice.VoiceInfo?.Name.Contains("Zira") == true ||
                    voice.VoiceInfo?.Name.Contains("David") == true)
                {
                    _synthesizer.SelectVoice(voice.VoiceInfo!.Name);
                    return;
                }
            }
        }
        catch
        {
            // 如果没有英语语音，用默认语音
        }
    }

    /// <summary>
    /// 朗读文本
    /// </summary>
    public void Speak(string text)
    {
        try
        {
            // 如果文本包含中文，先尝试检测语言
            _synthesizer.SpeakAsyncCancelAll();

            // 设置语速：-10 到 10
            _synthesizer.Rate = 0;
            // 设置音量：0 到 100
            _synthesizer.Volume = 100;

            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止朗读
    /// </summary>
    public void Stop()
    {
        _synthesizer.SpeakAsyncCancelAll();
    }

    public void Dispose()
    {
        _synthesizer?.Dispose();
    }
}
