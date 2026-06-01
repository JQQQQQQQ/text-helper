using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace TextHelper.Services;

public class GoogleTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;

    public GoogleTranslationService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<TranslationResult?> TranslateAsync(string text)
    {
        try
        {
            var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=zh-CN&dt=t&q=" + Uri.EscapeDataString(text);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var json = JArray.Parse(responseJson);

            var translation = json[0]?[0]?[0]?.ToString() ?? string.Empty;

            return new TranslationResult
            {
                Translation = translation,
                Explanation = "",
                Examples = Array.Empty<string>()
            };
        }
        catch (Exception ex)
        {
            return new TranslationResult
            {
                Translation = $"[翻译失败: {ex.Message}]",
                Explanation = "",
                Examples = Array.Empty<string>()
            };
        }
    }
}
