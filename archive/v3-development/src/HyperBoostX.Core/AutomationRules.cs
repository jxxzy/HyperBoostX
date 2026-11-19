namespace HyperBoostX.Core;

public sealed record AutomationRule(string Id,string Name,string Action,string Condition,int IntervalMinutes,int CooldownMinutes,bool Enabled,DateTimeOffset CreatedAt,DateTimeOffset? LastRunAt);
public sealed record AutomationRun(string Id,string RuleId,string RuleName,string Action,DateTimeOffset StartedAt,string Result,string Evidence);
public sealed record AutomationContext(bool OnAcPower,bool GamingSessionActive);

public sealed class AutomationRules(DocumentStore store)
{
    public static readonly string[] ApprovedActions=["SmartScan","CleanupScan","Reminder"];
    public static readonly string[] Conditions=["Always","OnAcPower","NotGaming"];
    public IReadOnlyList<AutomationRule> List()=>store.Read("automation-rules",new List<AutomationRule>()).OrderBy(r=>r.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
    public IReadOnlyList<AutomationRun> History()=>store.Read("automation-history",new List<AutomationRun>()).OrderByDescending(r=>r.StartedAt).Take(500).ToArray();
    public AutomationRule Add(string name,string action,string condition,int intervalMinutes,int cooldownMinutes)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Length>120||name.Any(char.IsControl))throw new HbxException("Enter a valid automation rule name.");
        if(!ApprovedActions.Contains(action,StringComparer.Ordinal))throw new HbxException("Automation action is not approved.");
        if(!Conditions.Contains(condition,StringComparer.Ordinal))throw new HbxException("Automation condition is not supported.");
        if(intervalMinutes<1||intervalMinutes>10_080)throw new HbxException("Automation interval must be between 1 minute and 7 days.");
        if(cooldownMinutes<intervalMinutes||cooldownMinutes>43_200)throw new HbxException("Cooldown must be at least the interval and no more than 30 days.");
        var rule=new AutomationRule(Guid.NewGuid().ToString("N"),name.Trim(),action,condition,intervalMinutes,cooldownMinutes,false,DateTimeOffset.UtcNow,null);
        store.Update("automation-rules",new List<AutomationRule>(),rules=>rules.Append(rule).TakeLast(200).ToList());return rule;
    }
    public void SetEnabled(string id,bool enabled)=>Update(id,rule=>rule with{Enabled=enabled});
    public void Remove(string id)=>store.Update("automation-rules",new List<AutomationRule>(),rules=>rules.Where(rule=>rule.Id!=id).ToList());
    public int EmergencyStop()
    {
        var count=0;store.Update("automation-rules",new List<AutomationRule>(),rules=>rules.Select(rule=>{if(rule.Enabled)count++;return rule with{Enabled=false};}).ToList());return count;
    }
    public IReadOnlyList<AutomationRule> Due(DateTimeOffset now,AutomationContext context)=>List().Where(rule=>rule.Enabled&&ConditionMet(rule.Condition,context)&&now-(rule.LastRunAt??rule.CreatedAt)>=TimeSpan.FromMinutes(rule.LastRunAt is null?rule.IntervalMinutes:rule.CooldownMinutes)).ToArray();
    public void Record(AutomationRule rule,DateTimeOffset startedAt,string result,string evidence)
    {
        if(result is not ("PASS" or "FAIL" or "SKIPPED"))throw new HbxException("Invalid automation result.");
        evidence=ReportExport.Redact(evidence);if(evidence.Length>1000)evidence=evidence[..1000];
        store.Update("automation-history",new List<AutomationRun>(),history=>history.Append(new(Guid.NewGuid().ToString("N"),rule.Id,rule.Name,rule.Action,startedAt,result,evidence)).TakeLast(500).ToList());
        Update(rule.Id,current=>current with{LastRunAt=startedAt});
    }
    private void Update(string id,Func<AutomationRule,AutomationRule> change)
    {
        var found=false;store.Update("automation-rules",new List<AutomationRule>(),rules=>rules.Select(rule=>{if(rule.Id!=id)return rule;found=true;return change(rule);}).ToList());if(!found)throw new HbxException("Automation rule was not found.");
    }
    private static bool ConditionMet(string condition,AutomationContext context)=>condition switch{"Always"=>true,"OnAcPower"=>context.OnAcPower,"NotGaming"=>!context.GamingSessionActive,_=>false};
}
