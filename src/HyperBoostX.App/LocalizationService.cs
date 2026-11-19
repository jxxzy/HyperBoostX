using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HyperBoostX.App;

public sealed record LanguageOption(string Code, string DisplayName);

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly LanguageOption[] Options =
    [
        new("id-ID", "Bahasa Indonesia"),
        new("en-US", "English"),
        new("es-ES", "Español"),
        new("pt-BR", "Português (Brasil)"),
        new("ja-JP", "日本語"),
        new("zh-CN", "简体中文")
    ];

    private readonly IReadOnlyDictionary<string, string> _fallback = Load("en-US");
    private IReadOnlyDictionary<string, string> _strings = new Dictionary<string, string>();

    public event PropertyChangedEventHandler? PropertyChanged;
    public static IReadOnlyList<LanguageOption> Languages { get; } = Array.AsReadOnly(Options);
    public string Language { get; private set; } = "id-ID";

    public string this[string key] =>
        _strings.TryGetValue(key, out var value) ? value :
        _fallback.TryGetValue(key, out value) ? value : $"[{key}]";

    public LocalizationService(string language = "id-ID") => SetLanguage(language);

    public void SetLanguage(string language)
    {
        var option = Options.SingleOrDefault(item =>
            string.Equals(item.Code, language, StringComparison.OrdinalIgnoreCase));
        if (option is null)
            throw new ArgumentException($"Unsupported language '{language}'.", nameof(language));

        var strings = option.Code == "en-US" ? _fallback : Load(option.Code);
        var missing = _fallback.Keys.Except(strings.Keys, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"The {option.Code} language pack is incomplete. Missing: {string.Join(", ", missing)}.");

        _strings = strings;
        Language = option.Code;
        var culture = CultureInfo.GetCultureInfo(option.Code);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        OnPropertyChanged(nameof(Language));
    }

    private static IReadOnlyDictionary<string, string> Load(string language)
    {
        var suffix = $".Assets.Localization.{language}.json";
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(suffix, StringComparison.Ordinal));
        if (resource is null)
            throw new InvalidOperationException($"The embedded {language} language pack is missing.");

        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The embedded {language} language pack could not be opened.");
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException($"The embedded {language} language pack is empty.");
        return strings;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
