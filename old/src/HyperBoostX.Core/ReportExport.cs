using System.Text.RegularExpressions;

namespace HyperBoostX.Core;

public static partial class ReportExport
{
    public static string Redact(string? text)
    {
        if(string.IsNullOrEmpty(text))return string.Empty;
        var profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if(!string.IsNullOrEmpty(profile))
        {
            text=text.Replace(profile,"[USERPROFILE]",StringComparison.OrdinalIgnoreCase);
            text=text.Replace(profile.Replace("\\","\\\\"),"[USERPROFILE]",StringComparison.OrdinalIgnoreCase);
        }
        text=UserPath().Replace(text,"[USERPROFILE]");
        text=Authorization().Replace(text,"Authorization=[REDACTED]");
        text=Secrets().Replace(text,"$1=[REDACTED]");
        return Ipv4().Replace(text,"[IP]");
    }
    public static DiagnosticReport Sanitize(DiagnosticReport report)=>report with
    {
        Facts=report.Facts.Select(f=>f with{Name=Redact(f.Name),Value=Redact(f.Value),Source=Redact(f.Source)}).ToArray(),
        Findings=report.Findings.Select(f=>f with{Title=Redact(f.Title),Evidence=Redact(f.Evidence),Recommendation=Redact(f.Recommendation)}).ToArray()
    };
    public static string Markdown(DiagnosticReport report)
    {
        var r=Sanitize(report);return $"# HyperBoostX — {r.Center}\n\nObserved: {r.ObservedAt:O}\n\n"+string.Join("\n\n",r.Facts.Select(f=>$"**{Redact(f.Name)}**\n\n{Redact(f.Value)}\n\nSource: {Redact(f.Source)} · {Redact(f.Quality)} · {Redact(f.Availability)}"))+
            (r.Findings.Count==0?"":"\n\n## Findings\n\n"+string.Join("\n\n",r.Findings.Select(f=>$"**{Redact(f.Title)}**\n\n{Redact(f.Evidence)}\n\n{Redact(f.Recommendation)}\n\nConfidence: {Redact(f.Confidence)}")));
    }
    public static string Csv(DiagnosticReport report)
    {
        static string Cell(string? s)
        {
            var value=s ?? string.Empty;
            var trimmed=value.TrimStart();
            var prefix=trimmed.Length>0 && ("=+-@".Contains(trimmed[0]) || value[0] is '\t' or '\r' or '\n') ? "'" : string.Empty;
            return "\""+prefix+value.Replace("\"","\"\"")+"\"";
        }
        return "Name,Value,Source,Quality,Availability\r\n"+string.Join("\r\n",Sanitize(report).Facts.Select(f=>string.Join(",",new[]{f.Name,f.Value,f.Source,f.Quality,f.Availability}.Select(Cell))));
    }
    public static string Html(DiagnosticReport report)
    {
        var r=Sanitize(report);static string Encode(string? value)=>System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
        return "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>HyperBoostX report</title><style>body{font:16px system-ui;margin:40px;color:#172026}table{border-collapse:collapse;width:100%}th,td{padding:12px;text-align:left;border:1px solid #ccd4d6;white-space:pre-wrap;overflow-wrap:anywhere}h1{font-size:28px}</style><body>"+
            $"<h1>HyperBoostX — {Encode(r.Center)}</h1><p>Observed: {r.ObservedAt:O}</p><table><thead><tr><th>Observation</th><th>Value</th><th>Source</th><th>Quality / availability</th></tr></thead><tbody>"+
            string.Join("",r.Facts.Select(f=>$"<tr><td>{Encode(f.Name)}</td><td>{Encode(f.Value)}</td><td>{Encode(f.Source)}</td><td>{Encode(f.Quality)} / {Encode(f.Availability)}</td></tr>"))+"</tbody></table>"+
            string.Join("",r.Findings.Select(f=>$"<section><h2>{Encode(f.Title)}</h2><p>{Encode(f.Evidence)}</p><p>{Encode(f.Recommendation)}</p><p>{Encode(f.Confidence)}</p></section>"))+"</body></html>";
    }
    [GeneratedRegex(@"[A-Za-z]:\\+(?:Users|Documents and Settings)\\+[^\\\s""/]+",RegexOptions.IgnoreCase)]private static partial Regex UserPath();
    [GeneratedRegex(@"\b(api[_-]?key|token|password|secret|authorization|bearer|webhook)\s*[:= ]\s*[^\s,;]+",RegexOptions.IgnoreCase)]private static partial Regex Secrets();
    [GeneratedRegex(@"\bauthorization\s*[:=]\s*(?:Bearer|Basic)\s+[^\s,;]+",RegexOptions.IgnoreCase)]private static partial Regex Authorization();
    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]private static partial Regex Ipv4();
}
