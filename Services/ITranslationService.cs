namespace TextHelper.Services;

public interface ITranslationService
{
    Task<TranslationResult?> TranslateAsync(string text);
}
