using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Views
{
    public class PlacementActionPageBase : UserControl
    {
        public async void RunPlacementAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: FeatureActionViewModel action } || DataContext is not CyberPageViewModel page)
                return;

            if (page.IsBusy)
                return;

            if (action.ConfirmationRequired || action.IsDestructive || IsMutatingApply(action))
            {
                var message = action.IsDestructive
                    ? "This action is guarded because it can affect system state. Safety Guard will still evaluate it and no unsupported change is forced."
                    : "Continue only after reviewing the preview output. Restore metadata and Safety Guard remain required.";
                if (!ConfirmMutatingAction(action.Label, message))
                    return;
            }

            await RunFeatureActionAsync(action);
        }

        private async Task RunFeatureActionAsync(FeatureActionViewModel action)
        {
            if (DataContext is not CyberPageViewModel page || page.IsBusy)
                return;

            page.IsBusy = true;
            SetActionButtonsEnabled(false);
            page.Status = $"Running {action.Label}...";
            page.LiveResultTitle = "Advanced Details";
            page.LiveResult = $"Calling {NormalizeMethod(action.Method)} {NormalizePath(action.Path)}...";
            page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (page is PlacementPageViewModel placement)
                placement.ResultSummary = $"Calling local backend route {NormalizePath(action.Path)}. Buttons are disabled until the request finishes.";

            try
            {
                using var client = new HyperBoostBackendClient();
                var method = NormalizeMethod(action.Method);
                object result = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
                    ? await client.PostJsonRouteAsync(NormalizePath(action.Path), action.Payload ?? new JObject())
                    : await client.GetJsonAsync(NormalizePath(action.Path));

                var token = NormalizeBackendResult(result);
                page.Status = BuildStatusText(action, token);
                page.LiveResult = BuildReadableRaw(action, token);
                page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (page is PlacementPageViewModel placementPage)
                    placementPage.ResultSummary = BuildSummary(token, fallback: $"{action.Label} completed. Review Advanced Details for the raw backend payload.");
                UpdateMetricsFromResult(page, token, action);
            }
            catch (Exception ex)
            {
                page.Status = $"{action.Label} failed safely";
                page.LiveResult = $"{action.Label}\n{NormalizeMethod(action.Method)} {NormalizePath(action.Path)}\n\n{BuildFriendlyError(action.Label, ex)}";
                page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (page is PlacementPageViewModel placementPage)
                    placementPage.ResultSummary = BuildFriendlyError(action.Label, ex);
            }
            finally
            {
                page.IsBusy = false;
                SetActionButtonsEnabled(true);
            }
        }

        private static bool ConfirmMutatingAction(string title, string message)
        {
            var result = MessageBox.Show(
                $"{message}\n\nContinue only if you reviewed the preview and approve this action.",
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            return result == MessageBoxResult.OK;
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            foreach (var button in FindVisualChildren<Button>(this).Where(button => button.Tag is FeatureActionViewModel))
            {
                var action = (FeatureActionViewModel)button.Tag;
                button.IsEnabled = enabled && action.IsEnabled;
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    yield return typed;

                foreach (var nested in FindVisualChildren<T>(child))
                    yield return nested;
            }
        }

        private static bool IsMutatingApply(FeatureActionViewModel action)
        {
            var id = action.Id ?? string.Empty;
            return string.Equals(NormalizeMethod(action.Method), "POST", StringComparison.OrdinalIgnoreCase) &&
                   (id.Contains(".apply.", StringComparison.OrdinalIgnoreCase) ||
                    id.Contains(".restore.", StringComparison.OrdinalIgnoreCase) ||
                    action.PreviewRequired ||
                    action.Restore);
        }

        private static string NormalizeMethod(string method) =>
            string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();

        private static string NormalizePath(string path) =>
            string.IsNullOrWhiteSpace(path)
                ? "/api/health"
                : path.Trim().StartsWith("/", StringComparison.Ordinal) ? path.Trim() : "/" + path.Trim();

        private static JToken NormalizeBackendResult(object result)
        {
            if (result == null)
                return JValue.CreateNull();
            if (result is JToken token)
                return token;
            return JToken.FromObject(result);
        }

        private static string BuildFriendlyError(string actionName, Exception ex)
        {
            var message = SensitiveTextRedactor.Redact(ex.Message ?? string.Empty);
            if (message.Contains("401", StringComparison.OrdinalIgnoreCase) || message.Contains("Unauthorized local session", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} was rejected by the local session guard. Relaunch HyperBoostX through HyperBoostX.exe so WPF and backend share the same token.";
            if (message.Contains("refused", StringComparison.OrdinalIgnoreCase) || message.Contains("No connection", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} could not reach the local backend. Start HyperBoostX through the launcher, then retry.";
            if (message.Contains("400", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} sent an invalid request payload. Run preview first or check the action map contract.";
            if (message.Contains("404", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} endpoint is unavailable in this build. This is a release blocker until the route is fixed or removed.";
            if (message.Contains("409", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} requires preview, approval, or restore metadata. Review the preview flow first.";
            if (message.Contains("blocked", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} was blocked by Safety Guard. No system change was applied. Detail: {message}";
            if (message.Contains("500", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} hit a backend error. No system change was applied; export diagnostics from Feature Audit.";
            return $"{actionName} stopped safely. No system change was applied. Detail: {message}";
        }

        private static string BuildStatusText(FeatureActionViewModel action, JToken token)
        {
            if (token is JObject obj)
            {
                var status = obj.Value<string>("status")?.Trim();
                var safetyDetail = BuildSafetyStatusDetail(obj);

                if (string.Equals(status, "blocked", StringComparison.OrdinalIgnoreCase) || obj.Value<bool?>("blocked") == true)
                    return string.IsNullOrWhiteSpace(safetyDetail)
                        ? $"{action.Label}: blocked by Safety Guard"
                        : $"{action.Label}: blocked by Safety Guard - {safetyDetail}";
                if (obj.Value<bool?>("ok") == false)
                    return string.IsNullOrWhiteSpace(safetyDetail)
                        ? $"{action.Label}: stopped safely, review required"
                        : $"{action.Label}: stopped safely - {safetyDetail}";
                if (string.Equals(status, "preview", StringComparison.OrdinalIgnoreCase))
                    return $"{action.Label}: preview ready, approval required before apply";
                if (string.Equals(status, "partial", StringComparison.OrdinalIgnoreCase))
                    return $"{action.Label}: partial result loaded";
                if (string.Equals(status, "admin_required", StringComparison.OrdinalIgnoreCase))
                    return $"{action.Label}: admin required for this Windows action";
                if (obj.Value<bool?>("success") == false)
                    return $"{action.Label}: approval or review required";
                if (ContainsTrueFlag(obj, "requires_approval") || ContainsTrueFlag(obj, "requires_user_approval"))
                    return $"{action.Label}: preview ready, approval required before apply";
                if (obj["items"] is JArray items)
                    return $"{action.Label}: loaded {items.Count} item(s)";
                if (obj["safe_actions"] is JArray safeActions)
                    return $"{action.Label}: {safeActions.Count} safe action(s) ready";
            }

            return $"{action.Label}: complete";
        }

        private static string BuildSafetyStatusDetail(JObject obj)
        {
            if (obj["blocked_reasons"] is JArray blockedReasons && blockedReasons.Count > 0)
                return SensitiveTextRedactor.Redact(blockedReasons[0]?.ToString() ?? string.Empty);

            var message = obj.Value<string>("message");
            if (!string.IsNullOrWhiteSpace(message))
                return SensitiveTextRedactor.Redact(message);

            var reason = obj.Value<string>("reason");
            return SensitiveTextRedactor.Redact(reason ?? string.Empty);
        }

        private static bool ContainsTrueFlag(JToken token, string key)
        {
            if (token is JObject obj)
            {
                if (obj[key]?.Type == JTokenType.Boolean && obj.Value<bool>(key))
                    return true;

                return obj.Properties().Any(property => ContainsTrueFlag(property.Value, key));
            }

            return token is JArray array && array.Any(item => ContainsTrueFlag(item, key));
        }

        private static string BuildReadableRaw(FeatureActionViewModel action, JToken token)
        {
            var body = SensitiveTextRedactor.Redact(token.ToString(Formatting.Indented));
            if (body.Length > 14000)
                body = body[..14000] + "\n... output truncated in UI ...";

            return $"{action.Label}\n{NormalizeMethod(action.Method)} {NormalizePath(action.Path)}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\nRaw JSON (redacted)\n{body}";
        }

        private static string BuildSummary(JToken token, string fallback)
        {
            if (token is not JObject obj)
                return fallback;

            var lines = new List<string>();
            AddIfPresent(lines, obj, "status");
            AddIfPresent(lines, obj, "message");
            AddIfPresent(lines, obj, "disclaimer");
            AddIfPresent(lines, obj, "current_version");
            AddIfPresent(lines, obj, "version");
            AddIfPresent(lines, obj, "creator_ready_score");
            AddIfPresent(lines, obj, "estimated_files");
            AddIfPresent(lines, obj, "estimated_size_mb");
            AddCount(lines, obj, "items");
            AddCount(lines, obj, "safe_actions");
            AddCount(lines, obj, "blocked_risky_actions");
            AddCount(lines, obj, "recommendations");

            return lines.Count == 0
                ? fallback
                : string.Join(Environment.NewLine, lines.Take(10));
        }

        private static void AddIfPresent(ICollection<string> lines, JObject obj, string key)
        {
            var value = obj[key];
            if (value != null && value.Type != JTokenType.Object && value.Type != JTokenType.Array)
                lines.Add($"- {key}: {SensitiveTextRedactor.Redact(value.ToString())}");
        }

        private static void AddCount(ICollection<string> lines, JObject obj, string key)
        {
            if (obj[key] is JArray array)
                lines.Add($"- {key}: {array.Count}");
        }

        private static void UpdateMetricsFromResult(CyberPageViewModel page, JToken token, FeatureActionViewModel action)
        {
            if (page.Metrics.Count == 0)
                return;

            var first = page.Metrics[0];
            first.Value = "LIVE";
            first.Detail = action.Label;
            first.Score = 92;

            if (token is not JObject obj || page.Metrics.Count < 2)
                return;

            var second = page.Metrics[1];
            if (obj["items"] is JArray items)
            {
                second.Value = items.Count.ToString();
                second.Detail = "Backend items loaded";
                second.Score = Math.Clamp(60 + items.Count, 60, 100);
            }
            else if (obj["safe_actions"] is JArray safeActions)
            {
                second.Value = safeActions.Count.ToString();
                second.Detail = "Safe actions ready";
                second.Score = 95;
            }
            else if (obj.Value<int?>("creator_ready_score") is int creatorScore)
            {
                second.Value = creatorScore.ToString();
                second.Detail = "Creator readiness";
                second.Score = Math.Clamp(creatorScore, 0, 100);
            }
        }
    }
}
