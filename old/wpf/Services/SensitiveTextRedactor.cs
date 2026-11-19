using System;
using System.Text.RegularExpressions;

namespace HyperBoostX.Services
{
    public static class SensitiveTextRedactor
    {
        private static readonly Regex JsonSensitiveStringRegex = new(
            "(\"(?:api[_-]?key|apikey|token|secret|license[_-]?key|session|authorization|password|email|username|user_name|user)\"\\s*:\\s*\")[^\"]*(\")",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex QuerySensitiveRegex = new(
            "([?&](?:api[_-]?key|apikey|token|secret|session|license[_-]?key|password)=)[^&\\s\"]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex EmailRegex = new(
            "\\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}\\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PlainUserProfileRegex = new(
            "\\b[A-Z]:\\\\Users\\\\[^\\\\\\r\\n\"]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex EscapedUserProfileRegex = new(
            "\\b[A-Z]:\\\\\\\\Users\\\\\\\\[^\\\\\\r\\n\"]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex KnownSecretRegex = new(
            "\\b(?:sk-[A-Za-z0-9_-]{16,}|ghp_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|xox[a-z]-[A-Za-z0-9-]{20,}|Bearer\\s+[A-Za-z0-9._~+\\-/]+=*)\\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex WebhookRegex = new(
            "https://hooks\\.slack\\.com/services/[^\\s\"]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Redact(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var result = value;
            result = ReplaceLiteral(result, Environment.UserName, "<USER>");
            result = ReplaceLiteral(result, Environment.MachineName, "<MACHINE>");
            result = ReplaceLiteral(result, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "<USER_PROFILE>");
            result = EscapedUserProfileRegex.Replace(result, "<USER_PROFILE>");
            result = PlainUserProfileRegex.Replace(result, "<USER_PROFILE>");
            result = JsonSensitiveStringRegex.Replace(result, "$1<REDACTED>$2");
            result = QuerySensitiveRegex.Replace(result, "$1<REDACTED>");
            result = EmailRegex.Replace(result, "<REDACTED_EMAIL>");
            result = WebhookRegex.Replace(result, "<REDACTED_WEBHOOK>");
            result = KnownSecretRegex.Replace(result, "<REDACTED_SECRET>");
            return result;
        }

        private static string ReplaceLiteral(string value, string needle, string replacement)
        {
            if (string.IsNullOrWhiteSpace(needle) || needle.Length < 3)
                return value;

            return Regex.Replace(value, Regex.Escape(needle), replacement, RegexOptions.IgnoreCase);
        }
    }
}
