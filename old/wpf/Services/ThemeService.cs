namespace HyperBoostX.Services
{
    public sealed class ThemeService
    {
        public string CurrentTheme { get; private set; } = "Cyber Dark";
        public string AccentColor { get; private set; } = "Cyan";
        public void Apply(string theme, string accent)
        {
            CurrentTheme = string.IsNullOrWhiteSpace(theme) ? "Cyber Dark" : theme;
            AccentColor = string.IsNullOrWhiteSpace(accent) ? "Cyan" : accent;
        }
    }
}
