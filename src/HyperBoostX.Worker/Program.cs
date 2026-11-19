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
                using var endpoint=new PrivilegedPipeEndpoint(sessionKey,new Dictionary<string,Func<string,string,bool>>(StringComparer.Ordinal)
                {
                    ["worker.health"]=(target,body)=>target.Length==0&&body=="{}",
                    ["worker.shutdown"]=(target,body)=>target.Length==0&&body=="{}"
                });
                for(var requestNumber=0;requestNumber<MaximumRequests&&!shutdown;requestNumber++)
                {
                    using var idleTimeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await endpoint.ServeOnceAsync(server,launch.ClientProcessId,launch.UserSid.Value,launch.ClientSessionId,Dispatch,TimeSpan.FromSeconds(10),idleTimeout.Token);
                }
                return shutdown?0:3;

                Task<PrivilegedDispatchResult> Dispatch(PrivilegedRequestEnvelope request,CancellationToken _)
                {
                    if(request.ActionId=="worker.shutdown"){shutdown=true;return Task.FromResult(new PrivilegedDispatchResult(true,"HBX-WORKER-SHUTDOWN","Worker shutdown accepted and will complete after this response."));}
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
