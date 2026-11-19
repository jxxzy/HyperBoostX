using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace HyperBoostX.Windows;

public sealed record PrivilegedPipeClientIdentity(uint ProcessId,uint SessionId,string UserSid,string Name);

public static class PrivilegedPipeIdentity
{
    public static PrivilegedPipeClientIdentity ReadConnectedClient(NamedPipeServerStream server)
    {
        ArgumentNullException.ThrowIfNull(server);
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("Privileged pipe identity requires Windows.");
        if(!server.IsConnected)throw new InvalidOperationException("The named-pipe client is not connected.");
        if(!GetNamedPipeClientProcessId(server.SafePipeHandle.DangerousGetHandle(),out var processId))throw new Win32Exception(Marshal.GetLastWin32Error(),"Unable to obtain the named-pipe client process ID.");
        if(!GetNamedPipeClientSessionId(server.SafePipeHandle.DangerousGetHandle(),out var sessionId))throw new Win32Exception(Marshal.GetLastWin32Error(),"Unable to obtain the named-pipe client session ID.");
        string? sid=null;string? name=null;
        server.RunAsClient(()=>
        {
            using var identity=WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
            sid=identity.User?.Value;name=identity.Name;
        });
        if(string.IsNullOrWhiteSpace(sid)||string.IsNullOrWhiteSpace(name))throw new InvalidOperationException("The named-pipe client identity could not be resolved.");
        return new(processId,sessionId,sid,name);
    }

    public static (uint ProcessId,uint SessionId) ReadConnectedServer(NamedPipeClientStream client)
    {
        ArgumentNullException.ThrowIfNull(client);
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("Privileged pipe identity requires Windows.");
        if(!client.IsConnected)throw new InvalidOperationException("The named-pipe server is not connected.");
        if(!GetNamedPipeServerProcessId(client.SafePipeHandle.DangerousGetHandle(),out var processId))throw new Win32Exception(Marshal.GetLastWin32Error(),"Unable to obtain the named-pipe server process ID.");
        if(!GetNamedPipeServerSessionId(client.SafePipeHandle.DangerousGetHandle(),out var sessionId))throw new Win32Exception(Marshal.GetLastWin32Error(),"Unable to obtain the named-pipe server session ID.");
        return(processId,sessionId);
    }

    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(IntPtr pipe,out uint clientProcessId);

    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientSessionId(IntPtr pipe,out uint clientSessionId);

    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(IntPtr pipe,out uint serverProcessId);

    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerSessionId(IntPtr pipe,out uint serverSessionId);
}
