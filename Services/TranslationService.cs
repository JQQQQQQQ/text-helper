using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TextHelper.Services;

/// <summary>
/// 翻译服务 — 调用 DeepSeek API 获取翻译和解释
/// </summary>
public class TranslationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public TranslationService(string apiKey, string model = "deepseek-chat")
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
        _model = model;
    }

    /// <summary>
    /// 翻译文本：返回中文翻译 + 解释 + 例句
    /// </summary>
    public async Task<TranslationResult?> TranslateAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "your-deepseek-api-key-here")
        {
            return new TranslationResult
            {
                Translation = "[请先在 appsettings.json 中配置 DeepSeek API Key]",
                Explanation = "",
                Examples = Array.Empty<string>()
            };
        }

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "你是一个英语学习助手。请将用户输入的英文翻译成中文，" +
                              "并给出简要的用法解释和两个例句。用 JSON 格式返回：" +
                              "{\"translation\": \"中文翻译\", \"explanation\": \"用法解释\", \"examples\": [\"例句1\", \"例句2\"]}"
                },
                new
                {
                    role = "user",
                    content = text
                }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/chat/completions");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // 通过代理发送（如果配置了）
        // 默认直连

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<DeepSeekResponse>(responseJson);

            if (result?.Choices != null && result.Choices.Length > 0)
            {
                var content = result.Choices[0].Message.Content;
                // 尝试解析 JSON 格式的回复
                try
                {
                    return JsonConvert.DeserializeObject<TranslationResult>(content);
                }
                catch
                {
                    // 如果 DeepSeek 没返回 JSON，直接作为翻译文本
                    return new TranslationResult
                    {
                        Translation = content,
                        Explanation = "",
                        Examples = Array.Empty<string>()
                    };
                }
            }
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

        return null;
    }

    private class DeepSeekResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        public MessageInfo Message { get; set; } = new();
    }

    private class MessageInfo
    {
        public string Content { get; set; } = "";
    }
}

/// <summary>
/// 翻译结果
/// </summary>
public class TranslationResult
{
    public string Translation { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string[] Examples { get; set; } = Array.Empty<string>();
}
