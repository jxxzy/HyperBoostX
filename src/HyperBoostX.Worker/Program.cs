using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using HyperBoostX.Core;
using HyperBoostX.Windows;

namespace HyperBoostX.Worker;

internal static class Program
{
    private const int MaximumRequests=64;
    private static async Task<int> Main(string[] args)
    {
        try
        {
            if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("HyperBoostX Worker requires Windows.");
            var launch=LaunchArguments.Parse(args);
            using var identity=WindowsIdentity.GetCurrent();var currentSid=identity.User??throw new HbxException("HBX-WORKER-IDENTITY: Worker SID is unavailable.");
            if(!currentSid.Equals(launch.UserSid))throw new HbxException("HBX-WORKER-IDENTITY: UAC credentials changed the Windows user; the worker refused the request.");
            await using var server=PrivilegedPipeFactory.CreateServer(launch.PipeName,launch.UserSid);
            using var connectTimeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await server.WaitForConnectionAsync(connectTimeout.Token);
            var sessionKey=await PrivilegedBootstrap.EstablishServerAsync(server,launch.ClientProcessId,launch.UserSid.Value,launch.ClientSessionId,launch.PipeName,connectTimeout.Token);
            try
            {
                var shutdown=false;
                Runtime? runtime=null;PrivilegedActionDispatcher? actionDispatcher=null;
                var allowlist=new Dictionary<string,Func<string,string,bool>>(PrivilegedActionDispatcher.PayloadValidators,StringComparer.Ordinal)
                {
                    ["worker.health"]=(target,body)=>target.Length==0&&body=="{}",
                    ["worker.shutdown"]=(target,body)=>target.Length==0&&body=="{}",
                    ["worker.power.inspect"]=(target,body)=>target.Length==0&&body=="{}"
                };
                using var endpoint=new PrivilegedPipeEndpoint(sessionKey,allowlist);
                for(var requestNumber=0;requestNumber<MaximumRequests&&!shutdown;requestNumber++)
                {
                    await endpoint.ServeOnceAsync(server,launch.ClientProcessId,launch.UserSid.Value,launch.ClientSessionId,Dispatch,
                        TimeSpan.FromSeconds(10),CancellationToken.None,frameReadTimeout:PrivilegedOperationBudget.FrameReadTimeout,
                        authenticatedOperationBudget:PrivilegedOperationBudget.DispatchTimeout);
                }
                return shutdown?0:3;

                Task<PrivilegedDispatchResult> Dispatch(PrivilegedRequestEnvelope request,CancellationToken cancellation)
                {
                    if(request.ActionId=="worker.shutdown"){shutdown=true;return Task.FromResult(new PrivilegedDispatchResult(true,"HBX-WORKER-SHUTDOWN","Worker shutdown accepted and will complete after this response."));}
                    if(request.ActionId=="worker.power.inspect")
                    {
                        cancellation.ThrowIfCancellationRequested();
                        var currentPlanId=PowerAction.Current();
                        var availablePlans=PowerAction.Plans();
                        var found=availablePlans.Any(plan=>string.Equals(plan.Id,currentPlanId,StringComparison.OrdinalIgnoreCase));
                        var resultBody=JsonSerializer.Serialize(new{ActivePlanId=currentPlanId,PlanCount=availablePlans.Count,ActivePlanEnumerated=found});
                        return Task.FromResult(new PrivilegedDispatchResult(true,"HBX-WORKER-POWER","Active power plan and plan inventory were read from Windows PowrProf.",resultBody));
                    }
                    if(request.ActionId.StartsWith("action.",StringComparison.Ordinal))
                    {
                        runtime??=new Runtime();actionDispatcher??=new PrivilegedActionDispatcher(runtime.CreatePrivilegedWorkerEngine(),runtime.Journal);
                        return actionDispatcher.DispatchAsync(request,cancellation);
                    }
                    using var process=Process.GetCurrentProcess();var principal=new WindowsPrincipal(identity);
                    var body=JsonSerializer.Serialize(new{ProcessId=Environment.ProcessId,SessionId=process.SessionId,UserSid=currentSid.Value,Elevated=principal.IsInRole(WindowsBuiltInRole.Administrator),Protocol=PrivilegedRequestValidator.ProtocolVersion});
                    return Task.FromResult(new PrivilegedDispatchResult(true,"HBX-WORKER-HEALTH","Worker identity and authenticated IPC are available.",body));
                }
            }
            finally{CryptographicOperations.ZeroMemory(sessionKey);}
        }
        catch(OperationCanceledException){return 2;}
        catch(Exception exception)
        {
            Console.Error.WriteLine(ReportExport.Redact(exception.Message));return 1;
        }
    }

    private sealed record LaunchArguments(string PipeName,uint ClientProcessId,uint ClientSessionId,SecurityIdentifier UserSid)
    {
        public static LaunchArguments Parse(string[] args)
        {
            if(args.Length!=8)throw new HbxException("HBX-WORKER-ARGS: Expected --pipe, --client-pid, --client-session, and --client-sid.");
            var values=new Dictionary<string,string>(StringComparer.Ordinal);
            for(var index=0;index<args.Length;index+=2)if(!values.TryAdd(args[index],args[index+1]))throw new HbxException("HBX-WORKER-ARGS: Duplicate launch argument.");
            if(values.Keys.Any(key=>key is not ("--pipe" or "--client-pid" or "--client-session" or "--client-sid")))throw new HbxException("HBX-WORKER-ARGS: Unknown launch argument.");
            if(!values.TryGetValue("--pipe",out var pipe)||string.IsNullOrWhiteSpace(pipe)||pipe.Length>200||pipe.IndexOfAny(['\\','/'])>=0)throw new HbxException("HBX-WORKER-ARGS: Pipe name is invalid.");
            if(!values.TryGetValue("--client-pid",out var pidText)||!uint.TryParse(pidText,out var pid)||pid==0)throw new HbxException("HBX-WORKER-ARGS: Client process ID is invalid.");
            if(!values.TryGetValue("--client-session",out var sessionText)||!uint.TryParse(sessionText,out var session))throw new HbxException("HBX-WORKER-ARGS: Client session ID is invalid.");
            SecurityIdentifier sid;
            try{sid=new SecurityIdentifier(values["--client-sid"]);}
            catch(Exception exception) when(exception is ArgumentException or KeyNotFoundException){throw new HbxException("HBX-WORKER-ARGS: Client SID is invalid.");}
            return new(pipe,pid,session,sid);
        }
    }
}
