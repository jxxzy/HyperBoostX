using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace HyperBoostX.Core;

public sealed record IntegrationSecrets(string NvidiaApiKey="",string DiscordWebhook="");
public sealed record NotificationRateLimit(DateTimeOffset LastAttempt);
public sealed record NotificationEvent(DateTimeOffset CreatedAt,string Category,string Code,string Delivery);
public sealed class Notifications(DocumentStore store,HttpClient http)
{
    private static readonly TimeSpan MinimumAttemptInterval=TimeSpan.FromMinutes(1);
    private readonly SemaphoreSlim _gate=new(1,1);
    public bool Enabled=>store.Read("notification-settings",false);
    public void SetEnabled(bool value)=>store.Update("notification-settings",false,_=>value);
    public IReadOnlyList<NotificationEvent> History=>store.Read("notifications",new List<NotificationEvent>()).AsEnumerable().Reverse().ToArray();
    public static Uri ValidateWebhook(string text)
    {
        if(!Uri.TryCreate(text,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!uri.IsDefaultPort||uri.Host!="discord.com"||uri.UserInfo!=""||uri.Query!=""||uri.Fragment!=""||
            !Regex.IsMatch(uri.AbsolutePath,@"^/api(?:/v\d+)?/webhooks/\d+/[A-Za-z0-9_-]+$"))throw new HbxException("HBX-NOTIFY-0001: Enter an HTTPS Discord webhook URL on discord.com.");
        return uri;
    }
    private void Record(string category,string code,string delivery)=>store.Update("notifications",new List<NotificationEvent>(),items=>items.Append(new(DateTimeOffset.UtcNow,category,code,delivery)).TakeLast(200).ToList());
    public async Task<string> ReportAsync(string category,string code,bool test,CancellationToken token)
    {
        // Only bounded category/code metadata is transmitted. Never send exception text or system reports.
        category=Regex.Replace(category,@"[^A-Za-z0-9 ._-]","");code=Regex.Replace(code,@"[^A-Za-z0-9._-]","");
        category=category[..Math.Min(category.Length,60)];code=code[..Math.Min(code.Length,80)];
        if(!Enabled){Record(category,code,"Local only — disabled");return "Local only — disabled";}
        await _gate.WaitAsync(token);
        try
        {
            var secret=store.Read("integration-secrets",new IntegrationSecrets()).DiscordWebhook;
            var uri=ValidateWebhook(secret);
            var now=DateTimeOffset.UtcNow;
            var reserved=false;
            store.Update("notification-rate-limit",new NotificationRateLimit(DateTimeOffset.MinValue),previous=>
            {
                if(now-previous.LastAttempt<MinimumAttemptInterval)return previous;
                reserved=true;
                return new NotificationRateLimit(now);
            });
            if(!reserved){Record(category,code,"Local only — rate limited");return "Local only — rate limited (60 seconds)";}
            var payload=Json.Write(new{content=$"HyperBoostX v3 {(test?"connection test":"notification")}\nCategory: {category}\nCode: {code}\nTime: {DateTimeOffset.UtcNow:O}",allowed_mentions=new{parse=Array.Empty<string>()}});
            for(var attempt=0;attempt<3;attempt++)
            {
                using var request=new HttpRequestMessage(HttpMethod.Post,uri){Content=new StringContent(payload,Encoding.UTF8,"application/json")};
                using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
                if(response.IsSuccessStatusCode){Record(category,code,"Delivered");return "Delivered";}
                if((response.StatusCode==HttpStatusCode.TooManyRequests||(int)response.StatusCode>=500)&&attempt<2)
                {
                    var seconds=response.Headers.RetryAfter?.Delta?.TotalSeconds??Math.Pow(2,attempt+1);
                    if(seconds>30){Record(category,code,"Local only — provider rate limited");return "Local only — provider rate limited";}
                    await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(seconds,1,30)),token);continue;
                }
                var status=$"Local fallback — HTTP {(int)response.StatusCode}";Record(category,code,status);return status;
            }
            Record(category,code,"Local fallback — retry limit");return "Local fallback — retry limit";
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){Record(category,code,"Local fallback — canceled");throw;}
        catch(HbxException e)
        {
            var errorCode=Regex.Match(e.Message,@"HBX-[A-Z]+-\d+").Value;
            var status=errorCode.Length>0?$"Local fallback — {errorCode}":"Local fallback — invalid configuration";
            Record(category,code,status);return status;
        }
        catch(HttpRequestException e)
        {
            var status=e.StatusCode is {} responseCode
                ?$"Local fallback — HTTP {(int)responseCode}"
                :$"Local fallback — connection error ({e.GetType().Name})";
            Record(category,code,status);return status;
        }
        catch(OperationCanceledException)
        {
            Record(category,code,"Local fallback — request timed out");return "Local fallback — request timed out";
        }
        finally{_gate.Release();}
    }
}
