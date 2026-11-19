namespace HyperBoostX.Core;

public static class SafePaths
{
    public static string Local(string path)
    {
        var full = Path.GetFullPath(path);
        if (full.StartsWith(@"\\", StringComparison.Ordinal) || full.IndexOf(':', 3) >= 0)
            throw new HbxException("HBX-PATH-0001: Network and alternate-stream paths are not allowed.");
        for (var p = full; !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
        {
            if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                throw new HbxException("HBX-PATH-0002: Reparse points and junctions are not allowed in managed storage.");
        }
        return full;
    }
    public static string Under(string root, string relative)
    {
        var canonicalRoot = Local(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Local(Path.Combine(canonicalRoot, relative));
        if (!full.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
            throw new HbxException("HBX-PATH-0003: Path escapes the managed root.");
        return full;
    }
    public static void AtomicWrite(string path, byte[] bytes)
    {
        Local(path);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { stream.Write(bytes); stream.Flush(true); }
            Local(path);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
