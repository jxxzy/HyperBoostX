using System.Collections.ObjectModel;
using System.Windows.Input;
using HyperBoostX.Core;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    public bool IsAiCenter=>SurfaceTitle=="AI Center";
    public bool IsNotifications=>SurfaceTitle=="Notifications";
    public bool IsIntegration=>IsAiCenter||IsNotifications;
    public ObservableCollection<string> AiModels {get;}=[];
    public ObservableCollection<ActionInfo> AiSuggestedActions {get;}=[];
    public ObservableCollection<NotificationEvent> NotificationHistory {get;}=[];
    private string _aiEndpoint=NvidiaProvider.DefaultEndpoint.AbsoluteUri,_aiQuestion="Jelaskan temuan dan rekomendasi yang aman.",_aiResult="",_aiModelFilter="";
    private string? _selectedAiModel;
    private ActionInfo? _selectedAiAction;
    public string AiEndpoint{get=>_aiEndpoint;set{_aiEndpoint=value;Notify();}}
    public string AiQuestion{get=>_aiQuestion;set{_aiQuestion=value;Notify();}}
    public string AiModelFilter{get=>_aiModelFilter;set{_aiModelFilter=value;Notify();}}
    public string AiResult{get=>_aiResult;private set{_aiResult=value;Notify();}}
    public string? SelectedAiModel{get=>_selectedAiModel;set{_selectedAiModel=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public ActionInfo? SelectedAiAction{get=>_selectedAiAction;set{_selectedAiAction=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public bool WebhookEnabled
    {
        get=>Runtime.Notifications.Enabled;
        set
        {
            try
            {
                if(value&&!HasValidWebhook)
                {
                    Status=Localization["WebhookConfigurationRequired"];
                    Notify();Notify(nameof(CanToggleWebhook));CommandManager.InvalidateRequerySuggested();return;
                }
                if(value&&!Confirm(Localization["WebhookConsent"],Localization["WebhookConsentTitle"])){Notify();return;}
                Runtime.Notifications.SetEnabled(value);Notify();Notify(nameof(CanToggleWebhook));Status=Localization[value?"WebhookEnabledStatus":"WebhookDisabledStatus"];
            }
            catch(Exception e){Status=ReportExport.Redact(e.Message);Notify();}
        }
    }
    public bool CanToggleWebhook=>WebhookEnabled||HasValidWebhook;
    private bool HasValidWebhook
    {
        get
        {
            var secret=Runtime.Documents.Read("integration-secrets",new IntegrationSecrets()).DiscordWebhook;
            if(string.IsNullOrWhiteSpace(secret))return false;
            try{Notifications.ValidateWebhook(secret);return true;}
            catch(HbxException){return false;}
        }
    }
    public string IntegrationCredentialStatus
    {
        get
        {
            var secrets=Runtime.Documents.Read("integration-secrets",new IntegrationSecrets());
            var webhook=secrets.DiscordWebhook.Length==0?Localization["NotConfigured"]:HasValidWebhook?Localization["StoredWithDpapi"]:Localization["InvalidReplaceIt"];
            return $"{Localization["NvidiaApiKey"]}: {(secrets.NvidiaApiKey.Length>0?Localization["StoredWithDpapi"]:Localization["NotConfigured"])} · {Localization["DiscordWebhook"]}: {webhook}";
        }
    }
    public ICommand DiscoverModelsCommand{get;private set;}=null!;
    public ICommand TestAiCommand{get;private set;}=null!;
    public ICommand AskAiCommand{get;private set;}=null!;
    public ICommand OfflineAdviceCommand{get;private set;}=null!;
    public ICommand ReviewAiActionCommand{get;private set;}=null!;
    public ICommand TestWebhookCommand{get;private set;}=null!;
    public ICommand ClearAiSecretCommand{get;private set;}=null!;
    public ICommand ClearWebhookCommand{get;private set;}=null!;
    private void InitializeIntegrations()
    {
        _aiEndpoint=Runtime.Journal.GetSetting("ai-endpoint",NvidiaProvider.DefaultEndpoint.AbsoluteUri);
        DiscoverModelsCommand=new Command(()=>Run(async t=>
        {
            var endpoint=NvidiaProvider.ValidateEndpoint(AiEndpoint);
            if(!Confirm($"Ambil katalog model dari {endpoint}? API key tersimpan dikirim sebagai header Authorization. Tidak ada hasil scan yang dikirim.","Provider connection"))return;
            var models=await Runtime.Ai.ModelsAsync(endpoint,AiKey(),t);AiModels.Clear();foreach(var model in models.Where(m=>m.Contains(AiModelFilter,StringComparison.OrdinalIgnoreCase)).Take(10))AiModels.Add(model);
            SelectedAiModel=AiModels.FirstOrDefault();Runtime.Journal.SetSetting("ai-endpoint",endpoint.AbsoluteUri);Status=$"{AiModels.Count} kandidat dari katalog provider. Test selected model memverifikasi akses chat; daftar katalog saja belum membuktikan kompatibilitas.";
        }),()=>!Busy);
        TestAiCommand=new Command(()=>Run(async t=>
        {
            if(SelectedAiModel==null)return;var endpoint=NvidiaProvider.ValidateEndpoint(AiEndpoint);
            if(!Confirm($"Kirim satu permintaan chat singkat ke {endpoint.Host}, model {SelectedAiModel}, untuk menguji kompatibilitas? Tidak ada hasil scan yang dikirim.","Test selected model"))return;
            var result=await Runtime.Ai.AdviseAsync(endpoint,AiKey(),SelectedAiModel,"Connection test: return a short explanation that the chat endpoint responded, with an empty action_ids array.",[],t);
            Runtime.Journal.SetSetting("ai-model",SelectedAiModel);AiResult=result.Explanation;Status="Provider mengembalikan respons chat. Model terpilih teruji untuk permintaan ini.";
        }),()=>!Busy&&SelectedAiModel!=null);
        AskAiCommand=new Command(()=>Run(AskAi),()=>!Busy&&SelectedAiModel!=null&&_report!=null);
        OfflineAdviceCommand=new Command(()=>{DisplayAdvice(NvidiaProvider.Offline(_report));Status="Analisis aturan lokal. Tidak ada data dikirim.";},()=>!Busy);
        ReviewAiActionCommand=new Command(()=>{if(SelectedAiAction is {} action){Navigate(action.Center);SelectedAction=Actions.FirstOrDefault(a=>a.Id==action.Id);Status="Tinjau target, risiko, dan Preview & Apply. AI belum mengubah Windows.";}},()=>!Busy&&SelectedAiAction!=null);
        TestWebhookCommand=new Command(()=>Run(async t=>{Status=await Runtime.Notifications.ReportAsync("ConnectionTest","HBX-TEST",true,t);RefreshNotifications();}),()=>!Busy&&WebhookEnabled&&HasValidWebhook);
        ClearAiSecretCommand=new Command(()=>Run(_=>{Runtime.Documents.Update("integration-secrets",new IntegrationSecrets(),s=>s with{NvidiaApiKey=""});AiModels.Clear();SelectedAiModel=null;Notify(nameof(IntegrationCredentialStatus));Status="API key lokal dihapus.";return Task.CompletedTask;}),()=>!Busy);
        ClearWebhookCommand=new Command(()=>Run(_=>{Runtime.Notifications.SetEnabled(false);Runtime.Documents.Update("integration-secrets",new IntegrationSecrets(),s=>s with{DiscordWebhook=""});Notify(nameof(WebhookEnabled));Notify(nameof(CanToggleWebhook));Notify(nameof(IntegrationCredentialStatus));CommandManager.InvalidateRequerySuggested();Status=Localization["WebhookRemovedStatus"];return Task.CompletedTask;}),()=>!Busy);
    }
    public void SaveAiSecret(string key)=>Run(_=>
    {
        NvidiaProvider.ValidateKey(key.Trim());
        Runtime.Documents.Update("integration-secrets",new IntegrationSecrets(),s=>s with{NvidiaApiKey=key.Trim()});Notify(nameof(IntegrationCredentialStatus));Status="API key tersimpan dengan DPAPI. Belum ada koneksi provider.";return Task.CompletedTask;
    });
    public void SaveWebhookSecret(string webhook)=>Run(_=>
    {
        var uri=Notifications.ValidateWebhook(webhook.Trim());Runtime.Documents.Update("integration-secrets",new IntegrationSecrets(),s=>s with{DiscordWebhook=uri.AbsoluteUri});Notify(nameof(IntegrationCredentialStatus));Notify(nameof(CanToggleWebhook));CommandManager.InvalidateRequerySuggested();Status=Localization["WebhookSavedStatus"];return Task.CompletedTask;
    });
    private string AiKey()=>Runtime.Documents.Read("integration-secrets",new IntegrationSecrets()).NvidiaApiKey;
    private async Task AskAi(CancellationToken token)
    {
        if(_report==null||SelectedAiModel==null)return;var endpoint=NvidiaProvider.ValidateEndpoint(AiEndpoint);
        var report=ReportExport.Sanitize(_report);
        var payload=Json.Write(new{question=ReportExport.Redact(AiQuestion),report.Center,report.ObservedAt,Facts=report.Facts.Take(30).Select(f=>new{f.Name,Value=f.Value[..Math.Min(f.Value.Length,160)],f.Quality,f.Availability}),Findings=report.Findings.Take(10)});
        if(payload.Length>20000)throw new HbxException("Preview terlalu panjang. Gunakan scan yang lebih spesifik atau pertanyaan lebih singkat.");
        if(!Confirm($"Data berikut akan dikirim ke {endpoint.Host} / {SelectedAiModel}. Redaksi bersifat best effort; periksa sebelum menyetujui.\n\n{payload}\n\nKirim?","Preview AI payload"))return;
        try
        {
            var advice=await Runtime.Ai.AdviseAsync(endpoint,AiKey(),SelectedAiModel,payload,Runtime.Engine.Actions,token);DisplayAdvice(advice);
            Runtime.Documents.Update("ai-history",new List<AiAdvice>(),items=>items.Append(advice).TakeLast(20).ToList());Status="Saran diterima. AI tidak memiliki akses eksekusi Windows.";
        }
        catch(Exception e)when(e is HbxException or System.Net.Http.HttpRequestException or System.Text.Json.JsonException)
        {DisplayAdvice(NvidiaProvider.Offline(_report));Status="Provider gagal: "+ReportExport.Redact(e.Message)+" Analisis lokal ditampilkan.";}
    }
    private void DisplayAdvice(AiAdvice advice)
    {
        AiResult=advice.Source+"\n\n"+advice.Explanation;AiSuggestedActions.Clear();foreach(var action in Runtime.Engine.Actions.Where(a=>advice.ActionIds.Contains(a.Id)))AiSuggestedActions.Add(action);SelectedAiAction=AiSuggestedActions.FirstOrDefault();
    }
    private void RefreshNotifications(){NotificationHistory.Clear();foreach(var item in Runtime.Notifications.History)NotificationHistory.Add(item);Notify(nameof(WebhookEnabled));Notify(nameof(IntegrationCredentialStatus));}
    private async Task RecordOperationFailure(Exception error)
    {
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));var match=System.Text.RegularExpressions.Regex.Match(error.Message,@"HBX-[A-Z]+-\d+");await Runtime.Notifications.ReportAsync("OperationFailure",match.Success?match.Value:error.GetType().Name,false,timeout.Token);}
        catch(Exception){Status+=" Riwayat notifikasi tidak dapat disimpan; periksa akses penyimpanan lokal.";}
    }
}
