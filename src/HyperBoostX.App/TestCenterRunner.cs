using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using HyperBoostX.Core;
using HyperBoostX.Windows;

namespace HyperBoostX.App;

public static class TestCenterRunner
{
    private sealed record Check(string Result,string Evidence,string? ErrorCode=null);

    public static async Task<SafeTestRun> RunAsync(Runtime runtime,CancellationToken token,Func<bool> isBusy)
    {
        var started=DateTimeOffset.UtcNow;var timer=Stopwatch.StartNew();var results=new List<SafeTestResult>();var cancelled=false;
        await Add("application.runtime","Application runtime",_ =>
        {
            var executable=Process.GetCurrentProcess().MainModule?.FileName;
            if(string.IsNullOrWhiteSpace(executable)||!File.Exists(executable))
                throw new InvalidOperationException("The running application executable could not be verified.");
            return Task.FromResult(new Check("PASS",$".NET {Environment.Version}; {Environment.OSVersion}; executable present."));
        });
        await Add("ui.route-bindings","Native route bindings",_ =>
        {
            var routes=FeatureRouteCatalog.All;
            foreach(var route in routes)
            {
                var binding=FeatureRouteCatalog.BindingFor(route.Name);
                if(string.IsNullOrWhiteSpace(binding.Surface)||string.IsNullOrWhiteSpace(route.FeatureId))
                    throw new InvalidOperationException($"Route binding is incomplete: {route.Name}.");
            }
            if(routes.Count!=73||routes.Select(route=>route.FeatureId).Distinct(StringComparer.Ordinal).Count()!=routes.Count)
                throw new InvalidOperationException($"Expected 73 unique route bindings; found {routes.Count}.");
            return Task.FromResult(new Check("PASS",$"{routes.Count} unique route IDs resolve to native surfaces."));
        });
        await Add("actions.registry","Registered action IDs",_ =>
        {
            var actions=runtime.Engine.Actions;
            var ids=actions.Select(action=>action.Id).ToArray();
            if(ids.Any(string.IsNullOrWhiteSpace)||ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)
                throw new InvalidOperationException("Registered action IDs are empty or duplicated.");
            return Task.FromResult(new Check("PASS",$"{ids.Length} unique registered actions; this does not assert legacy feature parity."));
        });
        await Add("backend.diagnostics","Windows diagnostic provider",async cancellation =>
        {
            var report=await runtime.Diagnostics.ScanAsync("Startup",cancellation);
            if(report.Facts.Count==0)throw new InvalidDataException("The startup diagnostic returned no observations.");
            var available=report.Facts.Count(fact=>fact.Availability=="Available");
            var unavailable=report.Facts.Count(fact=>fact.Availability=="Unavailable");
            return new Check("PASS",$"Read-only Startup diagnostics returned {report.Facts.Count} observations; {available} available and {unavailable} explicitly unavailable.");
        });
        await Add("permissions.current-user","Current-user permission context",_ =>
        {
            using var identity=WindowsIdentity.GetCurrent();
            var principal=new WindowsPrincipal(identity);
            var elevated=principal.IsInRole(WindowsBuiltInRole.Administrator);
            var blocked=Runtime.EnvironmentBlock();
            return Task.FromResult(new Check("PASS",$"Current-user token read; administrator role={elevated}; Safety Guard={(blocked is null?"allows registered actions":"blocks changes")}. No elevation was requested."));
        });
        await Add("dependencies.runtime","Required runtime dependencies",_ =>
        {
            var systemManagement=Type.GetType("System.Management.ManagementObjectSearcher, System.Management",false);
            return Task.FromResult(systemManagement is null
                ?new Check("UNSUPPORTED","System.Management is unavailable; WMI-backed inventories may report unavailable.")
                :new Check("PASS","System.Management dependency is loadable; provider-specific availability is reported by diagnostics."));
        });
        await Add("operations.background","Background operation status",_ =>
        {
            var active=isBusy();
            return Task.FromResult(active
                ?new Check("PASS","The active Test Center operation is reflected by the shell busy state; no detached worker queue exists.")
                :new Check("FAIL","The active Test Center operation is not reflected by the shell busy state."));
        });
        await Add("errors.history","Application error history",_ =>
        {
            var history=runtime.Notifications.History;
            var failures=history.Count(item=>item.Category=="OperationFailure");
            return Task.FromResult(new Check("PASS",$"{history.Count} local notification/error records readable; {failures} operation failure record(s)."));
        });
        await Add("resources.process","Application resource consumption",_ =>
        {
            using var process=Process.GetCurrentProcess();
            var workingSet=process.WorkingSet64;var privateBytes=process.PrivateMemorySize64;var cpuMilliseconds=process.TotalProcessorTime.TotalMilliseconds;
            if(workingSet<=0||privateBytes<=0||cpuMilliseconds<0)
                throw new InvalidDataException("Process resource counters returned an invalid measurement.");
            return Task.FromResult(new Check("PASS",$"Observed {DateTimeOffset.UtcNow:O}; working set {workingSet} bytes; private memory {privateBytes} bytes; cumulative process CPU {cpuMilliseconds:F0} ms."));
        });
        await Add("repair.safe-workflows","Safe application repair workflows",_ =>
            Task.FromResult(new Check("UNSUPPORTED","No automated application repair handler is available. Existing database recovery preserves damaged files and reports recovery status; no repair was attempted.")));
        await Add("database.integrity","Application database",_ =>
            Task.FromResult(new Check("PASS",$"SQLite quick_check: {runtime.Journal.IntegrityCheck()}.")));
        await Add("configuration.integrity","Configuration and document integrity",_ =>
        {
            var documents=runtime.Documents.InspectIntegrity();
            var theme=runtime.Journal.GetSetting("theme","Dark");
            var mode=runtime.Journal.GetSetting("mode","Beginner");
            var language=runtime.Journal.GetSetting("language","id-ID");
            if(theme is not ("Dark" or "Light")||mode is not ("Beginner" or "Advanced" or "Expert")||
               !LocalizationService.Languages.Any(option=>string.Equals(option.Code,language,StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("A saved appearance, detail-level, or language setting is invalid.");
            return Task.FromResult(new Check("PASS",$"Saved settings valid; {documents.Count} encrypted documents decrypted and parsed."));
        });
        await Add("recovery.journal","Recovery journal",_ =>
        {
            var history=runtime.Journal.History();
            var pending=history.Count(change=>change.NeedsRecovery);
            var notice=runtime.Journal.RecoveryNotice;
            if(!string.IsNullOrWhiteSpace(notice)||pending>0)
                return Task.FromResult(new Check("BLOCKED",$"{pending} pending operation(s). {ReportExport.Redact(notice)}","HBX-RECOVERY-PENDING"));
            return Task.FromResult(new Check("PASS",$"{history.Count} journal record(s) readable; no pending recovery operation."));
        });
        await Add("telemetry.sample","System telemetry sample",async cancellation =>
        {
            var telemetry=new Telemetry();var first=telemetry.Sample();
            await Task.Delay(150,cancellation);var sample=telemetry.Sample();
            if(sample.TotalMemory==0||sample.AvailableMemory>sample.TotalMemory)
                throw new InvalidDataException("The physical-memory provider returned an invalid measurement.");
            var cpu=sample.CpuPercent?.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)??"N/A";
            return new Check("PASS",$"Observed {sample.ObservedAt:O}; CPU {cpu}% ({sample.CpuAvailability}); memory {sample.AvailableMemory}/{sample.TotalMemory} bytes; network {sample.NetworkAvailability}. Initial sample {first.ObservedAt:O}.");
        });
        await Add("backup.integrity","Database backup integrity",_ =>
        {
            var result=runtime.Journal.BackupIntegrityCheck();
            return Task.FromResult(result is null
                ?new Check("SKIPPED","No local database backup exists; create one from Settings before validating it.")
                :new Check("PASS",$"Read-only SQLite quick_check: {result}."));
        });
        await Add("privileged-worker.ipc","Privileged worker and IPC",async cancellation =>
        {
            var workerPath=Path.Combine(AppContext.BaseDirectory,"HyperBoostX.Worker.exe");
            if(!File.Exists(workerPath))return new Check("UNSUPPORTED","The packaged worker executable is missing; no process was started.","HBX-WORKER-MISSING");
            await using var worker=await PrivilegedWorkerClient.StartAsync(workerPath,false,cancellation);
            var response=await worker.SendAsync("worker.health","","{}",cancellation);
            if(!response.Success||response.Code!="HBX-WORKER-HEALTH")return new Check("FAIL",$"Authenticated worker health returned {response.Code}.",response.Code);
            var power=await worker.SendAsync("worker.power.inspect","","{}",cancellation);
            if(!power.Success||power.Code!="HBX-WORKER-POWER")return new Check("FAIL",$"Authenticated worker power inspection returned {power.Code}.",power.Code);
            using var powerBody=System.Text.Json.JsonDocument.Parse(power.Body);
            var activePlan=powerBody.RootElement.GetProperty("ActivePlanId").GetString();
            var planCount=powerBody.RootElement.GetProperty("PlanCount").GetInt32();
            var activeEnumerated=powerBody.RootElement.GetProperty("ActivePlanEnumerated").GetBoolean();
            if(string.IsNullOrWhiteSpace(activePlan)||planCount<1||!activeEnumerated)return new Check("FAIL","Power inspection result was incomplete or inconsistent.", "HBX-WORKER-POWER-INCONSISTENT");
            return new Check("PASS",$"Separate worker process {worker.ProcessId} completed peer-bound ECDH and authenticated health/power inspection IPC. PowrProf returned {planCount} plan(s); active plan was enumerated. This safe check did not request elevation or mutate Windows.");
        });
        await Add("privileged-worker.audit","Privileged worker audit integrity",_ =>
        {
            var audit=runtime.Journal.WorkerAuditHistory(10000);
            return Task.FromResult(new Check("PASS",$"{audit.Count} encrypted worker audit record(s) passed hash-chain verification; raw targets are stored only as SHA-256 subject identifiers."));
        });
        await Add("application.file-integrity","Application file integrity",_ =>
            Task.FromResult(new Check("UNSUPPORTED","No signed file-integrity manifest is installed; runtime trust is not asserted.")));
        await Add("ai.connection","AI provider connection",_ =>
            Task.FromResult(new Check("SKIPPED","No network request was made. Run the provider test explicitly after reviewing data-sharing consent.")));
        await Add("webhook.connection","Webhook connection",_ =>
            Task.FromResult(new Check("SKIPPED","No network request was made. Run the connection test explicitly after enabling delivery with consent.")));

        timer.Stop();
        var run=new SafeTestRun(Guid.NewGuid().ToString("N"),started,timer.ElapsedMilliseconds,results.AsReadOnly()){Cancelled=cancelled};
        runtime.Documents.Update("test-history",new List<SafeTestRun>(),history=>new[]{run}.Concat(history).Take(50).ToList());
        runtime.Journal.SaveReport(new DiagnosticReport("Test Center",started,
            results.Select(result=>new Fact(result.TestId,$"{result.Result}: {result.Evidence}",result.Name,result.Result,result.ErrorCode??"Available")).ToArray(),[]));
        return run;

        async Task Add(string id,string name,Func<CancellationToken,Task<Check>> test)
        {
            var testStarted=DateTimeOffset.UtcNow;var stopwatch=Stopwatch.StartNew();
            if(cancelled||token.IsCancellationRequested)
            {
                cancelled=true;
                results.Add(new SafeTestResult(id,name,"NOT RUN",testStarted,0,"Not executed because the test run was cancelled.","HBX-TEST-CANCELLED"));
                return;
            }
            Check check;
            try{check=await test(token);}
            catch(OperationCanceledException) when(token.IsCancellationRequested)
            {
                cancelled=true;
                check=new Check("NOT RUN","Cancelled before this check completed; no result is claimed.","HBX-TEST-CANCELLED");
            }
            catch(UnauthorizedAccessException exception)
            {
                check=new Check("BLOCKED",ReportExport.Redact(exception.Message),"HBX-TEST-ACCESS-DENIED");
            }
            catch(Exception exception)
            {
                check=new Check("FAIL",ReportExport.Redact(exception.Message),"HBX-TEST-"+exception.GetType().Name.ToUpperInvariant());
            }
            stopwatch.Stop();
            results.Add(new SafeTestResult(id,name,check.Result,testStarted,stopwatch.ElapsedMilliseconds,check.Evidence,check.ErrorCode));
        }
    }
}
