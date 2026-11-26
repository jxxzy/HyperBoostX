using System.Collections.Concurrent;
using System.Diagnostics;

namespace HyperBoostX.Core;

public sealed class SafetyGuard(Func<string?> environmentBlock)
{
    private static readonly string[] Protected = ["defender","security","antivirus","firewall","vanguard","vgc","vgtray","battleye","easyanticheat","faceit","gameguard","nvidia","radeon","realtek","intel","audio","driver","armoury","msi","corsair","icue","tryx","kanali","trcc","him os","usb_lcd","usblcd","synaptics","touchpad"];
    public static bool ProtectedStartup(string name, string command) => Protected.Any(x => (name + " " + command).Contains(x,StringComparison.OrdinalIgnoreCase));
    public void Check(ISystemAction action, string target, bool rollback = false)
    {
        action.Validate(target);
        if (!rollback && environmentBlock() is { } reason) throw new HbxException(reason);
        if (!action.Info.Reversible) throw new HbxException("HBX-SAFE-0001: This engine requires a verified recovery strategy.");
    }
}

public sealed class ActionEngine
{
    private readonly Dictionary<string,ISystemAction> _actions;
    private readonly ConcurrentDictionary<string,(Preview Preview,long Tick)> _previews = new();
    private readonly object _previewGate = new();
    private readonly Journal _journal;
    private readonly SafetyGuard _safety;
    private readonly string _owner;
    public IReadOnlyList<ActionInfo> Actions => _actions.Values.Select(a=>a.Info).ToArray();
    public ActionEngine(IEnumerable<ISystemAction> actions, Journal journal, SafetyGuard safety, string owner)
    { _actions = actions.ToDictionary(a=>a.Info.Id,StringComparer.Ordinal); _journal=journal; _safety=safety; _owner=owner; }
    private ISystemAction Get(string id) => _actions.TryGetValue(id,out var a) ? a : throw new HbxException("HBX-SAFE-0002: Action is not registered.");
    public async Task<Preview> PreviewAsync(string id,string target,CancellationToken token)
    {
        var action=Get(id); _safety.Check(action,target);
        var before=await action.ReadAsync(target,token);
        token.ThrowIfCancellationRequested();
        _safety.Check(action,target); // The environment can become restricted during provider inspection.
        var desired=action.Desired(target,before);
        token.ThrowIfCancellationRequested();
        var preview=new Preview(id,target,before,desired,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,action.Info.Description);
        lock(_previewGate)
        {
            foreach(var pair in _previews.Where(p=>Stopwatch.GetElapsedTime(p.Value.Tick)>TimeSpan.FromMinutes(10)).ToArray()) _previews.TryRemove(pair.Key,out _);
            if (_previews.Count>=200) throw new HbxException("HBX-ACTION-0001: Too many outstanding previews.");
            _previews[preview.Token]=(preview,Stopwatch.GetTimestamp());
        }
        return preview;
    }
    public async Task<Change> ApplyAsync(string previewToken,bool consent,string? session,CancellationToken token)
    {
        if (!consent) throw new HbxException("HBX-ACTION-0002: Explicit approval is required.");
        if (!_previews.TryRemove(previewToken,out var pending) || Stopwatch.GetElapsedTime(pending.Tick)>TimeSpan.FromMinutes(10))
            throw new HbxException("HBX-ACTION-0003: Preview expired. Scan and review again.");
        var p=pending.Preview; var action=Get(p.ActionId); _safety.Check(action,p.Target);
        using var gate=_journal.Acquire(); // Cross-process, covers preview recheck, backup, mutation and verification.
        var current=await action.ReadAsync(p.Target,token);
        token.ThrowIfCancellationRequested();
        _safety.Check(action,p.Target); // Recheck after the provider read, while holding the operation lock.
        if(current!=p.Before) throw new HbxException("HBX-ACTION-0004: State changed after preview. Nothing was applied.");
        var c=new Change { ActionId=p.ActionId,Target=p.Target,Before=current,Desired=p.Desired,Owner=_owner,Session=session??Guid.NewGuid().ToString("N") };
        if(current==p.Desired)
        { c=c with{Status="AlreadyDesiredState",Observed=current,CompletedAt=DateTimeOffset.UtcNow,Message="No write was needed."};_journal.Save(c);return c; }
        token.ThrowIfCancellationRequested();
        _journal.SaveVerifiedIntent(c); // Both recovery representations must be readable and match.
        c=c with{Status="Applying"};_journal.SaveVerifiedIntent(c);
        try
        {
            token.ThrowIfCancellationRequested();
            _safety.Check(action,p.Target);
            await action.WriteAsync(c.Target,c.Desired,token);
            var after=await action.ReadAsync(c.Target,CancellationToken.None);
            if(after!=c.Desired) throw new HbxException("HBX-VERIFY-0001: Read-back did not match the requested state.");
            c=c with{Status="Verified",Observed=after,CompletedAt=DateTimeOffset.UtcNow,Message="Requested configuration was read back and verified. Performance improvement has not been measured."};
            _journal.Save(c);return c;
        }
        catch(Exception e)
        {
            try
            {
                var actual=await action.ReadAsync(c.Target,CancellationToken.None);
                if(actual==c.Desired)
                {
                    // A restore provider may throw after successfully writing the original state.
                    // Always read back before deciding recovery failed; never blindly retry a write.
                    try { await action.WriteAsync(c.Target,c.Before,CancellationToken.None); }
                    catch(Exception restoreWriteError)
                    {
                        try
                        {
                            actual=await action.ReadAsync(c.Target,CancellationToken.None);
                            if(actual!=c.Before&&!action.IsRestoreSatisfied(actual,c.Before))
                                throw new AggregateException("Restore write failed and original state was not verified.",restoreWriteError);
                        }
                        catch(HbxException){throw;}
                        catch(Exception readError)
                        {
                            throw new AggregateException("Restore write and verification failed.",restoreWriteError,readError);
                        }
                    }
                    actual=await action.ReadAsync(c.Target,CancellationToken.None);
                }
                var cancelled=e is OperationCanceledException;
                var targetNoLongerExists=action.IsRestoreSatisfied(actual,c.Before);
                var restored=actual==c.Before||targetNoLongerExists;
                var message=cancelled
                    ?actual==c.Before?"Operation cancelled; the original state was restored and verified."
                    :targetNoLongerExists?"Operation cancelled; the target no longer exists, so its process-specific state no longer applies."
                    :"Operation cancelled; recovery is required because the original state could not be verified."
                    :targetNoLongerExists?e.Message+" The target no longer exists; its process-specific state no longer applies."
                    :e.Message;
                c=c with
                {
                    Status=restored?(cancelled?"CancelledRestored":"FailedRestored"):"RecoveryRequired",
                    Observed=actual,
                    CompletedAt=DateTimeOffset.UtcNow,
                    Message=message
                };
            }
            catch(Exception restoreError) { c=c with{Status="RecoveryRequired",Message=$"{e.Message} Recovery: {restoreError.Message}"}; }
            _journal.Save(c);
            if(e is OperationCanceledException)throw;
            return c;
        }
    }
    public async Task<Change> RollbackAsync(string id,bool consent,CancellationToken token)
    {
        if(!consent) throw new HbxException("HBX-RESTORE-0003: Restore requires explicit approval.");
        using var gate=_journal.Acquire();var c=_journal.Find(id);
        if(c.Owner!=_owner) throw new HbxException("HBX-RESTORE-0004: This record belongs to a different machine or Windows user.");
        if(!c.Recoverable) throw new HbxException("HBX-RESTORE-0005: This record does not require rollback.");
        var action=Get(c.ActionId);action.ValidateRestore(c.Target,c.Before);
        if(!action.Info.Reversible)throw new HbxException("HBX-SAFE-0001: This engine requires a verified recovery strategy.");
        State current;
        try
        {
            current=await action.ReadAsync(c.Target,token);
        }
        catch(OperationCanceledException) when(token.IsCancellationRequested)
        {
            // No restore write has started. Preserve the durable recovery intent
            // and do not attempt an unapproved, uncancellable provider read.
            c=c with{Status="RollbackCancelled",Message="Rollback cancelled during provider inspection. Retry recovery."};
            _journal.Save(c);return c;
        }
        // A provider may complete its read while cancellation is requested. Do not
        // transition the journal to RollingBack or write after that cancellation.
        if(token.IsCancellationRequested)
        {
            c=c with{Status="RollbackCancelled",Observed=current,Message="Rollback cancelled after provider inspection. Retry recovery."};
            _journal.Save(c);return c;
        }
        var targetNoLongerExists=action.IsRestoreSatisfied(current,c.Before);
        if(current!=c.Before && current!=c.Desired && !targetNoLongerExists)
        {
            c=c with{Status="ConflictDetected",Observed=current,Message="External change detected. Current state was preserved; automatic restore is blocked."};
            _journal.Save(c);return c;
        }
        c=c with{Status="RollingBack"};_journal.SaveVerifiedIntent(c);
        try
        {
            token.ThrowIfCancellationRequested();
            if(current!=c.Before&&!targetNoLongerExists) await action.WriteAsync(c.Target,c.Before,token);
            var actual=await action.ReadAsync(c.Target,CancellationToken.None);
            var restored=actual==c.Before||action.IsRestoreSatisfied(actual,c.Before);
            c=c with{Status=restored?"RolledBack":"RollbackFailed",Observed=actual,CompletedAt=DateTimeOffset.UtcNow,Message=actual==c.Before?"Original state read back and verified.":restored?"Target no longer exists; its process-specific state no longer applies.":"Original state could not be verified."};
        }
        catch(OperationCanceledException e)
        {
            State? actual=null;Exception? readError=null;
            try{actual=await action.ReadAsync(c.Target,CancellationToken.None);}
            catch(Exception exception){readError=exception;}
            if(readError is not null)
                c=c with{Status="RecoveryRequired",Message=$"Rollback was cancelled and the resulting state could not be read: {readError.Message}"};
            else if(actual==c.Before||actual is not null&&action.IsRestoreSatisfied(actual,c.Before))
                c=c with{Status="RolledBack",Observed=actual,CompletedAt=DateTimeOffset.UtcNow,Message=actual==c.Before?"Rollback completed and verified as cancellation was requested.":"Target no longer exists; its process-specific state no longer applies."};
            else if(actual==c.Desired)
                c=c with{Status="RollbackCancelled",Observed=actual,Message=$"Rollback cancelled before the original state was restored. Retry recovery. {e.Message}"};
            else
                c=c with{Status="ConflictDetected",Observed=actual,Message="The state changed during rollback. Current state was preserved; inspect before retrying."};
            _journal.Save(c);return c;
        }
        catch(Exception e)
        {
            // A process may exit between identity validation and the restore write, or a provider
            // may throw after applying the original state. Only a fresh observation can resolve it.
            try
            {
                var actual=await action.ReadAsync(c.Target,CancellationToken.None);
                var satisfied=actual==c.Before||action.IsRestoreSatisfied(actual,c.Before);
                c=c with{Status=satisfied?"RolledBack":"RollbackFailed",Observed=actual,
                    CompletedAt=satisfied?DateTimeOffset.UtcNow:c.CompletedAt,
                    Message=actual==c.Before?"Original state read back and verified after a provider error.":
                        satisfied?"Target no longer exists; its process-specific state no longer applies.":e.Message};
            }
            catch(Exception readError){c=c with{Status="RecoveryRequired",Message=$"{e.Message} Recovery read-back: {readError.Message}"};}
        }
        _journal.Save(c);return c;
    }
    public sealed record BoostBatchResult(string Session,string Status,string Message,IReadOnlyList<Change> Changes);
    public static IReadOnlyList<string> SafeBoostActionIds(bool transparency,bool capture)
    {
        var ids=new List<string>();
        if(transparency)ids.Add("windows.visual.transparency.disable");
        if(capture)ids.Add("gaming.capture.disable");
        return ids;
    }

    // Persistent changes use the same action journal and recovery engine as individual actions.
    // This is a compensating batch, not a Windows transaction.
    public async Task<BoostBatchResult> ApplySafeBoostAsync(IReadOnlyList<Preview> previews,bool consent,CancellationToken token)
    {
        if(!consent)throw new HbxException("HBX-BOOST-0001: Explicit batch approval is required.");
        token.ThrowIfCancellationRequested();
        var allowed=SafeBoostActionIds(true,true);
        if(previews.Count is <1 or >2||previews.Any(p=>p.Target!=""||!allowed.Contains(p.ActionId,StringComparer.Ordinal)||Get(p.ActionId).Info.RequiresElevation)||
           previews.Select(p=>p.ActionId).Distinct(StringComparer.Ordinal).Count()!=previews.Count)
            throw new HbxException("HBX-BOOST-0002: Invalid safe boost action selection.");
        // Check the engine-issued immutable preview before any modification, not merely its displayed fields.
        foreach(var preview in previews)
            if(!_previews.TryGetValue(preview.Token,out var pending)||pending.Preview!=preview||
                Stopwatch.GetElapsedTime(pending.Tick)>TimeSpan.FromMinutes(10))
                throw new HbxException("HBX-BOOST-0003: Batch preview expired or does not match its original.");
        // Reject stale selections before any batch mutation. ApplyAsync retains its
        // locked read-back recheck against races after this non-mutating preflight.
        foreach(var preview in previews)
        {
            token.ThrowIfCancellationRequested();
            var action=Get(preview.ActionId);
            _safety.Check(action,preview.Target);
            var current=await action.ReadAsync(preview.Target,token);
            token.ThrowIfCancellationRequested();
            _safety.Check(action,preview.Target); // A restriction may appear while the provider is reading.
            if(current!=preview.Before)
                throw new HbxException("HBX-BOOST-0004: Configuration changed after preview. Nothing was applied; review all selections again.");
        }
        var session=Guid.NewGuid().ToString("N");
        var results=new List<Change>();
        try
        {
            foreach(var preview in previews)
            {
                token.ThrowIfCancellationRequested();
                var change=await ApplyAsync(preview.Token,true,session,token);
                results.Add(change);
                if(change.Status is not ("Verified" or "AlreadyDesiredState"))
                    throw new HbxException("Action did not verify: "+change.ActionId);
            }
            return new(session,"Verified","Selected configuration was verified. Performance improvement has not been measured.",results);
        }
        catch(Exception operationError)
        {
            var problems=new List<string>();
            try
            {
                // Recovery files are authoritative even when a database commit failed.
                _journal.Reconcile();
                if(!string.IsNullOrWhiteSpace(_journal.RecoveryNotice))
                    problems.Add("Recovery journal requires review: "+_journal.RecoveryNotice);
                foreach(var record in _journal.SessionHistory(session).Where(c=>c.Recoverable))
                {
                    try
                    {
                        var restored=await RollbackAsync(record.Id,true,CancellationToken.None);
                        results.RemoveAll(c=>c.Id==restored.Id);results.Add(restored);
                        if(restored.Status!="RolledBack")problems.Add(restored.ActionId+": "+restored.Status);
                    }
                    catch(Exception e){problems.Add(record.ActionId+": "+e.GetType().Name);}
                }
            }
            catch(Exception e){problems.Add("Recovery history unavailable: "+e.GetType().Name);}
            if(problems.Count!=0)return new(session,"RecoveryRequired","Batch interrupted. Review Restore: "+string.Join("; ",problems),results);
            return new(session,operationError is OperationCanceledException?"CancelledRestored":"FailedRestored",
                "Batch interrupted; recorded changes requiring recovery were restored. "+operationError.GetType().Name,results);
        }
    }

    public async Task<State> VerifyAsync(string id,CancellationToken token)
    {
        var c=_journal.Find(id);if(c.Owner!=_owner)throw new HbxException("HBX-RESTORE-0004: Owner mismatch.");
        var action=Get(c.ActionId);return await action.ReadAsync(c.Target,token);
    }
}
