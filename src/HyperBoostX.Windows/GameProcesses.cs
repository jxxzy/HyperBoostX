using System.Diagnostics;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class GameProcesses
{
    public static DiagnosticReport Scan(IReadOnlyList<GameEntry> games,CancellationToken token)
    {
        var facts=new List<Fact>();var inaccessible=0;
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if(!games.Any(g=>string.Equals(Path.GetFileNameWithoutExtension(g.Executable),process.ProcessName,StringComparison.OrdinalIgnoreCase)))continue;
                var path=process.MainModule?.FileName;
                foreach(var game in games.Where(g=>string.Equals(g.Executable,path,StringComparison.OrdinalIgnoreCase)))
                    facts.Add(new(game.Name,$"Running · PID {process.Id}","Exact executable path match"));
            }
            catch(Exception e)when(e is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException){inaccessible++;}
        }
        if(facts.Count==0)facts.Add(new("Running games","No accessible process matched a saved game executable.","Process API"));
        if(inaccessible>0)facts.Add(new("Unavailable processes",inaccessible.ToString(),"Process API","Unavailable","Unavailable"));
        return new("Game Library",DateTimeOffset.UtcNow,facts,[]);
    }
    public static Process Launch(GameEntry game)
    {
        var path=GameLibrary.ValidateExecutable(game.Executable);
        return Process.Start(new ProcessStartInfo(path){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(path)!,WindowStyle=ProcessWindowStyle.Normal})??throw new HbxException("HBX-GAME-0012: Game did not start.");
    }
}
