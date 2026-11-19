using HyperBoostX.Core;
using HyperBoostX.Windows;

try
{
    var arguments=args.ToList();string? root=null;var idx=arguments.IndexOf("--data-dir");
    if(idx>=0){if(idx+1>=arguments.Count)throw new ArgumentException("Missing data directory.");root=arguments[idx+1];arguments.RemoveRange(idx,2);}
    if(arguments.Count==0||arguments[0] is "help" or "--help")
    {
        Console.WriteLine("""
            HyperBoostX v3 recovery CLI
            hbx scan [Center]             Read-only scan (default: Smart Scan)
            hbx health                    Read-only dashboard
            hbx actions                   Registered change descriptions
            hbx registry                  Runtime feature and handoff inventory
            hbx reports                   Saved local reports (may contain private data)
            hbx ping <host>               Six ICMP probes; saves a local report
            hbx telemetry                 Two live samples of CPU/RAM/network
            hbx benchmark cpu             Three-second SHA-256 throughput test
            hbx benchmark storage --yes   64 MiB scratch-file write/read test
            hbx games                     Local registered games
            hbx gaming-session            Current gaming session state
            hbx gaming-restore --yes      Restore a pending gaming session
            hbx cleanup-scan              Read-only old-temp candidate scan
            hbx history                   Recent changes (contains local state; do not share raw)
            hbx verify <change-id>         Read current state without changing it
            hbx rollback <change-id> --yes Restore after drift validation
            hbx backup                    Back up local SQLite database
            hbx self-check                Check database and recovery state
            hbx report <output.json>      Export redacted system report
            Optional: --data-dir <path> selects an isolated application data directory.
            GUI and CLI use the same Safety Guard and recovery engine.
            """);return 0;
    }
    var runtime=new Runtime(root);using var cts=new CancellationTokenSource(TimeSpan.FromMinutes(3));
    Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;cts.Cancel();};
    switch(arguments[0])
    {
        case "scan":case "health":
            var report=await runtime.Diagnostics.ScanAsync(arguments[0]=="health"?"Dashboard":arguments.Count>1?arguments[1]:"Smart Scan",cts.Token);
            runtime.Journal.SaveReport(report);Console.WriteLine(Json.Write(report));break;
        case "actions":Console.WriteLine(Json.Write(runtime.Engine.Actions));break;
        case "registry":Console.WriteLine(Json.Write(new{SchemaVersion=1,ActionRegistryVersion=1,Actions=runtime.Engine.Actions,DiagnosticCenters=Diagnostics.Centers,Handoffs=Diagnostics.Centers.Append("Repair").Select(c=>new{Center=c,Tools=Handoffs.For(c)})}));break;
        case "history":Console.WriteLine(Json.Write(runtime.Journal.History()));break;
        case "reports":Console.WriteLine(Json.Write(runtime.Journal.Reports()));break;
        case "games":Console.WriteLine(Json.Write(runtime.Games.List()));break;
        case "gaming-session":Console.WriteLine(Json.Write(runtime.GamingSessions.Current));break;
        case "gaming-restore" when arguments.Count==2&&arguments[1]=="--yes":
            var restored=await runtime.GamingSessions.RestoreAsync(true,cts.Token);Console.WriteLine(Json.Write(restored));return restored.Status=="Restored"?0:2;
        case "cleanup-scan":Console.WriteLine(Json.Write(runtime.Cleanup.Scan(cts.Token)));break;
        case "telemetry":
            var telemetry=new Telemetry();telemetry.Sample();await Task.Delay(1000,cts.Token);Console.WriteLine(Json.Write(telemetry.Sample()));break;
        case "benchmark" when arguments.Count==2&&arguments[1]=="cpu":
            var cpu=await LocalBenchmarks.CpuAsync(cts.Token);runtime.Journal.SaveReport(cpu);Console.WriteLine(Json.Write(cpu));break;
        case "benchmark" when arguments.Count==3&&arguments[1]=="storage"&&arguments[2]=="--yes":
            var disk=await LocalBenchmarks.StorageAsync(runtime.Journal.Root,cts.Token);runtime.Journal.SaveReport(disk);Console.WriteLine(Json.Write(disk));break;
        case "ping" when arguments.Count==2:
            var ping=await Diagnostics.PingAsync(arguments[1].Trim(),cts.Token);runtime.Journal.SaveReport(ping);Console.WriteLine(Json.Write(ping));break;
        case "verify" when arguments.Count==2:Console.WriteLine(Json.Write(await runtime.Engine.VerifyAsync(arguments[1],cts.Token)));break;
        case "rollback" when arguments.Count==3&&arguments[2]=="--yes":
            var result=await runtime.Engine.RollbackAsync(arguments[1],true,cts.Token);Console.WriteLine(Json.Write(result));return result.Status=="RolledBack"?0:2;
        case "backup":runtime.Journal.Backup();Console.WriteLine("Database backup completed.");break;
        case "self-check":Console.WriteLine(Json.Write(new{Database="Integrity checked",Recovery=runtime.Journal.RecoveryNotice,Changes=runtime.Journal.History().Count,Pending=runtime.Journal.History().Count(c=>c.NeedsRecovery),MutationPolicy=Runtime.EnvironmentBlock()??"Registered changes allowed with preview and consent",Release="Development; Stable gates pending"}));break;
        case "report" when arguments.Count==2:
            var r=await runtime.Diagnostics.ScanAsync("Smart Scan",cts.Token);await File.WriteAllTextAsync(arguments[1],Json.Write(ReportExport.Sanitize(r)),cts.Token);Console.WriteLine("Redacted report saved.");break;
        default:throw new ArgumentException("Unsupported command or missing arguments. Run hbx --help.");
    }
    return 0;
}
catch(Exception e){Console.Error.WriteLine(ReportExport.Redact(e.Message));return 1;}
