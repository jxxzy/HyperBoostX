using System.Text;
using System.Text.Json;

namespace HyperBoostX.Core;

// Domain data is encrypted separately from recovery records. Updates share the journal lock.
public sealed class DocumentStore(Journal journal,IStateProtector protector)
{
    private string PathFor(string name)
    {
        if(name.Length is <1 or >64||name.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'))throw new HbxException("HBX-DOC-0001: Invalid document name.");
        var folder=SafePaths.Under(journal.Root,"Documents");Directory.CreateDirectory(folder);
        return SafePaths.Under(folder,name+".hbx");
    }
    public T Read<T>(string name,T fallback)
    {
        var path=PathFor(name);
        return File.Exists(path)?Json.Read<T>(Encoding.UTF8.GetString(protector.Unprotect(File.ReadAllBytes(path)))):fallback;
    }
    public T Update<T>(string name,T fallback,Func<T,T> update)
    {
        using var gate=journal.Acquire();var result=update(Read(name,fallback));
        SafePaths.AtomicWrite(PathFor(name),protector.Protect(Encoding.UTF8.GetBytes(Json.Write(result))));return result;
    }
    public IReadOnlyList<string> InspectIntegrity()
    {
        var folder=SafePaths.Under(journal.Root,"Documents");
        if(!Directory.Exists(folder))return [];
        if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)
            throw new InvalidDataException("HBX-DOC-0002: Document directory is a reparse point.");
        var documents=new List<string>();
        foreach(var file in Directory.EnumerateFiles(folder,"*.hbx",SearchOption.TopDirectoryOnly))
        {
            if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)
                throw new InvalidDataException("HBX-DOC-0003: A document is a reparse point.");
            var name=Path.GetFileNameWithoutExtension(file);
            var protectedBytes=File.ReadAllBytes(PathFor(name));
            var plaintext=protector.Unprotect(protectedBytes);
            using var document=JsonDocument.Parse(plaintext);
            documents.Add(name);
        }
        return documents.AsReadOnly();
    }
}
