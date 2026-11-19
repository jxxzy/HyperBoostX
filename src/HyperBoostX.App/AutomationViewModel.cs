using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using HyperBoostX.Core;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    private DispatcherTimer? _automationTimer;
    private AutomationRule? _selectedAutomationRule;
    public bool IsAutomation=>Title is "Scheduled Automation" or "Task & Rule System";
    public ObservableCollection<AutomationRule> AutomationRuleItems {get;}=[];
    public ObservableCollection<AutomationRun> AutomationHistory {get;}=[];
    public string AutomationName{get;set;}="Scheduled scan";
    public string AutomationAction{get;set;}="SmartScan";
    public string AutomationCondition{get;set;}="Always";
    public int AutomationIntervalMinutes{get;set;}=60;
    public int AutomationCooldownMinutes{get;set;}=60;
    public IReadOnlyList<string> AutomationActions=>AutomationRules.ApprovedActions;
    public IReadOnlyList<string> AutomationConditions=>AutomationRules.Conditions;
    public AutomationRule? SelectedAutomationRule{get=>_selectedAutomationRule;set{_selectedAutomationRule=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public ICommand AddAutomationRuleCommand{get;private set;}=null!;
    public ICommand EnableAutomationRuleCommand{get;private set;}=null!;
    public ICommand PauseAutomationRuleCommand{get;private set;}=null!;
    public ICommand RemoveAutomationRuleCommand{get;private set;}=null!;
    public ICommand EmergencyStopAutomationCommand{get;private set;}=null!;

    private void InitializeAutomation()
    {
        AddAutomationRuleCommand=new Command(()=>Run(_=>{var rule=Runtime.Automation.Add(AutomationName,AutomationAction,AutomationCondition,AutomationIntervalMinutes,AutomationCooldownMinutes);RefreshAutomation(rule.Id);Status="Automation rule saved in paused state. Enable it explicitly to opt in.";return Task.CompletedTask;}),()=>!Busy);
        EnableAutomationRuleCommand=new Command(()=>{if(SelectedAutomationRule is {} rule){Runtime.Automation.SetEnabled(rule.Id,true);RefreshAutomation(rule.Id);Status="Automation rule enabled while HyperBoostX is running."; }},()=>!Busy&&SelectedAutomationRule is {Enabled:false});
        PauseAutomationRuleCommand=new Command(()=>{if(SelectedAutomationRule is {} rule){Runtime.Automation.SetEnabled(rule.Id,false);RefreshAutomation(rule.Id);Status="Automation rule paused."; }},()=>!Busy&&SelectedAutomationRule is {Enabled:true});
        RemoveAutomationRuleCommand=new Command(()=>{if(SelectedAutomationRule is {} rule&&Confirm($"Remove automation rule '{rule.Name}'? Its execution history is retained.","Remove automation rule")){Runtime.Automation.Remove(rule.Id);RefreshAutomation();Status="Automation rule removed."; }},()=>!Busy&&SelectedAutomationRule is not null);
        EmergencyStopAutomationCommand=new Command(()=>{var count=Runtime.Automation.EmergencyStop();RefreshAutomation();Status=$"Emergency stop paused {count} enabled automation rule(s).";},()=>!Busy&&AutomationRuleItems.Any(rule=>rule.Enabled));
        _automationTimer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(30)};_automationTimer.Tick+=async(_,_)=>await EvaluateAutomationAsync();_automationTimer.Start();
    }
    private void LoadAutomationPage(){if(IsAutomation)RefreshAutomation();}
    private void RefreshAutomation(string? selectedId=null)
    {
        selectedId??=SelectedAutomationRule?.Id;AutomationRuleItems.Clear();foreach(var rule in Runtime.Automation.List())AutomationRuleItems.Add(rule);SelectedAutomationRule=AutomationRuleItems.FirstOrDefault(rule=>rule.Id==selectedId)??AutomationRuleItems.FirstOrDefault();
        AutomationHistory.Clear();foreach(var run in Runtime.Automation.History())AutomationHistory.Add(run);CommandManager.InvalidateRequerySuggested();
    }
    private async Task EvaluateAutomationAsync()
    {
        if(Busy)return;var context=new AutomationContext(System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus!=System.Windows.Forms.PowerLineStatus.Offline,Runtime.GamingSessions.Active);
        foreach(var rule in Runtime.Automation.Due(DateTimeOffset.UtcNow,context))
        {
            var started=DateTimeOffset.UtcNow;
            try
            {
                string evidence;
                switch(rule.Action)
                {
                    case "SmartScan":var report=await Runtime.Diagnostics.ScanAsync("Smart Scan",default);Runtime.Journal.SaveReport(report);evidence=$"{report.Facts.Count} observations, {report.Findings.Count} findings";break;
                    case "CleanupScan":var cleanup=Runtime.Cleanup.Scan(default);evidence=$"{cleanup.Items.Count} candidates; discovery only, no files changed";break;
                    case "Reminder":evidence="Local reminder became due while HyperBoostX was running.";break;
                    default:throw new HbxException("Automation action is not approved.");
                }
                Runtime.Automation.Record(rule,started,"PASS",evidence);Status=$"Automation '{rule.Name}' completed: {evidence}";
            }
            catch(Exception exception){Runtime.Automation.Record(rule,started,"FAIL",exception.GetType().Name);Status=$"Automation '{rule.Name}' failed: {ReportExport.Redact(exception.Message)}";}
        }
        if(IsAutomation)RefreshAutomation();
    }
}
