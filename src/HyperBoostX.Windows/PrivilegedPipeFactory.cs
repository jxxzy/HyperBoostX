using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace HyperBoostX.Windows;

public static class PrivilegedPipeFactory
{
    public static NamedPipeServerStream CreateServer(string pipeName,SecurityIdentifier authorizedUser)
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("Privileged named pipes require Windows.");
        if(string.IsNullOrWhiteSpace(pipeName)||pipeName.Length>200||pipeName.IndexOfAny(['\\','/'])>=0)
            throw new ArgumentException("Pipe name is invalid.",nameof(pipeName));
        ArgumentNullException.ThrowIfNull(authorizedUser);
        var security=new PipeSecurity();
        security.SetAccessRuleProtection(true,false);
        security.SetOwner(authorizedUser);
        security.AddAccessRule(new PipeAccessRule(authorizedUser,PipeAccessRights.ReadWrite,AccessControlType.Allow));
        return NamedPipeServerStreamAcl.Create(pipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous|PipeOptions.WriteThrough,PrivilegedFrameBufferSize,PrivilegedFrameBufferSize,security,HandleInheritability.None,
            (PipeAccessRights)0);
    }

    private const int PrivilegedFrameBufferSize=16*1024+sizeof(int);
}
