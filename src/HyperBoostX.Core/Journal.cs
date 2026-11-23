using System.Text;
using Microsoft.Data.Sqlite;

namespace HyperBoostX.Core;

public sealed class Journal
{
    private readonly string _database;
    private readonly IStateProtector _protector;
    public string Root { get; }
    public string RecoveryNotice { get; private set; } = "";
    public Journal(string root, IStateProtector protector)
    {
        Root = SafePaths.Local(root); _protector = protector;
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(SafePaths.Under(Root, "Recovery"));
        _database = SafePaths.Under(Root, "hyperboostx.db");
        using var gate = Acquire();
        try { Initialize(); }
        catch (SqliteException e) when (e.SqliteErrorCode is 11 or 26)
        {
            SqliteConnection.ClearAllPools();
            var suffix = ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            foreach (var file in new[] { _database, _database + "-wal", _database + "-shm" })
                if (File.Exists(file)) File.Move(file, file + suffix);
            Initialize();
            RecoveryNotice = "The damaged database was preserved. Action history was recovered from encrypted recovery records. Review settings and restore history.";
            foreach (var change in RecoveryRecords()) SaveDatabase(change);
        }
    }
    public FileStream Acquire() => new(SafePaths.Under(Root,"operation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    private SqliteConnection Open()
    {
        SafePaths.Local(_database);
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _database, Pooling = false, DefaultTimeout = 5 }.ToString());
        try
        {
            c.Open();
            using var cmd = c.CreateCommand(); cmd.CommandText = "PRAGMA synchronous=FULL; PRAGMA busy_timeout=5000;"; cmd.ExecuteNonQuery();
            return c;
        }
        catch { c.Dispose(); throw; }
    }
    private void Initialize()
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA quick_check";
        if (!string.Equals(cmd.ExecuteScalar()?.ToString(), "ok", StringComparison.Ordinal))
            throw new SqliteException("HBX-DATA-0002: Database integrity failed.",11);
        cmd.CommandText = "PRAGMA user_version";
        if (Convert.ToInt32(cmd.ExecuteScalar()) > 2) throw new HbxException("HBX-DATA-0003: Database is newer than this application.");
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS changes(sequence INTEGER PRIMARY KEY AUTOINCREMENT,id TEXT NOT NULL UNIQUE,payload BLOB NOT NULL);
            CREATE TABLE IF NOT EXISTS events(sequence INTEGER PRIMARY KEY AUTOINCREMENT,change_id TEXT NOT NULL,status TEXT NOT NULL,utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS settings(key TEXT PRIMARY KEY,value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS reports(id TEXT PRIMARY KEY,utc TEXT NOT NULL,payload BLOB NOT NULL);
            CREATE TABLE IF NOT EXISTS worker_audit(sequence INTEGER PRIMARY KEY AUTOINCREMENT,id TEXT NOT NULL UNIQUE,utc TEXT NOT NULL,payload BLOB NOT NULL);
            PRAGMA user_version=2;
            """;
        cmd.ExecuteNonQuery();
    }
    public string IntegrityCheck()
    {
        using var c=Open();using var cmd=c.CreateCommand();
        cmd.CommandText="PRAGMA quick_check";
        var result=cmd.ExecuteScalar()?.ToString();
        if(!string.Equals(result,"ok",StringComparison.Ordinal))
            throw new HbxException("HBX-DATA-0002: Database integrity check failed.");
        return "ok";
    }
    public string? BackupIntegrityCheck()
    {
        var path=SafePaths.Under(Root,"hyperboostx.backup.db");
        if(!File.Exists(path))return null;
        using var c=new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource=SafePaths.Local(path),Mode=SqliteOpenMode.ReadOnly,Pooling=false,DefaultTimeout=5
        }.ToString());
        c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA quick_check";
        var result=cmd.ExecuteScalar()?.ToString();
        if(!string.Equals(result,"ok",StringComparison.Ordinal))
            throw new HbxException("HBX-DATA-0005: Backup database integrity check failed.");
        return result;
    }
    public void Save(Change change)
    {
        if (!Guid.TryParseExact(change.Id,"N",out _)) throw new HbxException("HBX-DATA-0004: Invalid change ID.");
        var data = _protector.Protect(Encoding.UTF8.GetBytes(Json.Write(change)));
        // Durable intent remains readable even if the SQLite commit fails after an OS mutation.
        SafePaths.AtomicWrite(SafePaths.Under(Root, $"Recovery/{change.Id}.hbx"), data);
        SaveDatabase(change);
    }
    private void SaveDatabase(Change change)
    {
        using var c = Open(); using var tx = c.BeginTransaction(); using var cmd = c.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO changes(id,payload) VALUES($id,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload; INSERT INTO events(change_id,status,utc) VALUES($id,$status,$utc);";
        cmd.Parameters.AddWithValue("$id",change.Id); cmd.Parameters.AddWithValue("$payload",_protector.Protect(Encoding.UTF8.GetBytes(Json.Write(change))));
        cmd.Parameters.AddWithValue("$status",change.Status); cmd.Parameters.AddWithValue("$utc",DateTimeOffset.UtcNow.ToString("O")); cmd.ExecuteNonQuery(); tx.Commit();
    }
    private Change Decode(byte[] bytes) => Json.Read<Change>(Encoding.UTF8.GetString(_protector.Unprotect(bytes)));
    private IEnumerable<Change> RecoveryRecords()
    {
        var records=new List<Change>();var failed=0;
        foreach(var file in Directory.EnumerateFiles(SafePaths.Under(Root,"Recovery"),"*.hbx"))
        {
            try
            {
                var change=Decode(File.ReadAllBytes(SafePaths.Local(file)));
                if(!Guid.TryParseExact(change.Id,"N",out _)||!string.Equals(Path.GetFileNameWithoutExtension(file),change.Id,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Recovery ID does not match its file.");
                records.Add(change);
            }
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException or InvalidDataException)
            {failed++;}
        }
        if(failed>0)RecoveryNotice=$"{failed} recovery record(s) could not be read and were preserved. Other history remains available; damaged records cannot be restored automatically.";
        return records.OrderBy(c=>c.StartedAt);
    }
    public IReadOnlyList<Change> History(int limit = 200)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT payload FROM changes ORDER BY sequence DESC LIMIT $n"; cmd.Parameters.AddWithValue("$n",Math.Clamp(limit,1,10000));
        using var r = cmd.ExecuteReader(); var results = new List<Change>();
        while(r.Read()) results.Add(Decode((byte[])r[0]));
        return results;
    }
    // Recovery must not silently omit older changes when global history exceeds
    // the UI history cap. Encrypted payloads require filtering after decoding.
    public IReadOnlyList<Change> SessionHistory(string sessionId)
    {
        if (!Guid.TryParseExact(sessionId,"N",out _)) throw new HbxException("HBX-GAME-0016: Invalid session ID.");
        using var c=Open();using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT payload FROM changes ORDER BY sequence DESC";
        using var reader=cmd.ExecuteReader();var changes=new List<Change>();
        while(reader.Read())
        {
            var change=Decode((byte[])reader[0]);
            if(change.Session==sessionId)changes.Add(change);
        }
        return changes;
    }
    public Change Find(string id)
    {
        if (!Guid.TryParseExact(id,"N",out _)) throw new HbxException("HBX-RESTORE-0001: Invalid restore ID.");
        var file = SafePaths.Under(Root,$"Recovery/{id}.hbx");
        if (File.Exists(file))
        {
            var change=Decode(File.ReadAllBytes(file));
            if(change.Id!=id)throw new HbxException("HBX-RESTORE-0006: Recovery record ID mismatch.");
            return change;
        }
        throw new HbxException("HBX-RESTORE-0002: Recovery record unavailable.");
    }
    public void Reconcile()
    {
        using var gate = Acquire();
        foreach(var change in RecoveryRecords())
        {
            using var c = Open(); using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT payload FROM changes WHERE id=$id";
            cmd.Parameters.AddWithValue("$id",change.Id);
            var existing = cmd.ExecuteScalar() as byte[];
            if (existing is null || Decode(existing) != change) SaveDatabase(change);
        }
    }
    public string GetSetting(string key, string fallback = "")
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key=$key"; cmd.Parameters.AddWithValue("$key",key);
        return cmd.ExecuteScalar()?.ToString() ?? fallback;
    }
    public void SetSetting(string key, string value)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value";
        cmd.Parameters.AddWithValue("$key",key); cmd.Parameters.AddWithValue("$value",value); cmd.ExecuteNonQuery();
    }
    public void SetSettings(IReadOnlyDictionary<string,string> settings)
    {
        using var c=Open();using var tx=c.BeginTransaction();
        foreach(var (key,value) in settings)
        {
            if(string.IsNullOrWhiteSpace(key)||key.Length>64||value is null||value.Length>4096)
                throw new HbxException("HBX-DATA-0006: Invalid settings batch.");
            using var cmd=c.CreateCommand();cmd.Transaction=tx;
            cmd.CommandText="INSERT INTO settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value";
            cmd.Parameters.AddWithValue("$key",key);cmd.Parameters.AddWithValue("$value",value);cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public void SaveReport(DiagnosticReport report)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO reports VALUES($id,$utc,$payload); DELETE FROM reports WHERE id NOT IN (SELECT id FROM reports ORDER BY utc DESC LIMIT 100);";
        cmd.Parameters.AddWithValue("$id",Guid.NewGuid().ToString("N")); cmd.Parameters.AddWithValue("$utc",report.ObservedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$payload",_protector.Protect(Encoding.UTF8.GetBytes(Json.Write(report))));cmd.ExecuteNonQuery();
    }
    public IReadOnlyList<DiagnosticReport> Reports(int limit=100)
    {
        using var c=Open();using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT payload FROM reports ORDER BY utc DESC LIMIT $n";
        cmd.Parameters.AddWithValue("$n",Math.Clamp(limit,1,100));
        using var reader=cmd.ExecuteReader();var reports=new List<DiagnosticReport>();
        while(reader.Read())reports.Add(Json.Read<DiagnosticReport>(Encoding.UTF8.GetString(_protector.Unprotect((byte[])reader[0]))));
        return reports;
    }
    public WorkerAuditRecord SaveWorkerAudit(string operation,string subject,bool success,string code)
    {
        if(string.IsNullOrWhiteSpace(operation)||operation.Length>128||string.IsNullOrWhiteSpace(code)||code.Length>128)
            throw new HbxException("HBX-AUDIT-0001: Worker audit fields are invalid.");
        using var gate=Acquire();using var c=Open();using var tx=c.BeginTransaction();
        var previous="GENESIS";using(var last=c.CreateCommand())
        {
            last.Transaction=tx;last.CommandText="SELECT payload FROM worker_audit ORDER BY sequence DESC LIMIT 1";
            if(last.ExecuteScalar() is byte[] payload)previous=Json.Read<WorkerAuditRecord>(Encoding.UTF8.GetString(_protector.Unprotect(payload))).Hash;
        }
        var id=Guid.NewGuid().ToString("N");var observed=DateTimeOffset.UtcNow;var subjectHash=Json.Hash(subject??"");
        var hash=Json.Hash($"HBX-WORKER-AUDIT-V1\n{id}\n{observed:O}\n{operation}\n{subjectHash}\n{success}\n{code}\n{previous}");
        var record=new WorkerAuditRecord(id,observed,operation,subjectHash,success,code,previous,hash);
        using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO worker_audit(id,utc,payload) VALUES($id,$utc,$payload)";
        cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$utc",observed.ToString("O"));cmd.Parameters.AddWithValue("$payload",_protector.Protect(Encoding.UTF8.GetBytes(Json.Write(record))));cmd.ExecuteNonQuery();tx.Commit();
        return record;
    }
    public IReadOnlyList<WorkerAuditRecord> WorkerAuditHistory(int limit=200)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT payload FROM worker_audit ORDER BY sequence ASC";
        using var reader=cmd.ExecuteReader();var all=new List<WorkerAuditRecord>();var previous="GENESIS";
        while(reader.Read())
        {
            var record=Json.Read<WorkerAuditRecord>(Encoding.UTF8.GetString(_protector.Unprotect((byte[])reader[0])));
            var expected=Json.Hash($"HBX-WORKER-AUDIT-V1\n{record.Id}\n{record.ObservedAt:O}\n{record.Operation}\n{record.SubjectHash}\n{record.Success}\n{record.Code}\n{previous}");
            if(record.PreviousHash!=previous||record.Hash!=expected)throw new HbxException("HBX-AUDIT-0002: Worker audit chain integrity failed.");
            all.Add(record);previous=record.Hash;
        }
        return all.TakeLast(Math.Clamp(limit,1,10000)).Reverse().ToArray();
    }
    public void Backup()
    {
        using var gate = Acquire(); using var source = Open();
        var path = SafePaths.Under(Root,"hyperboostx.backup.db");
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=path,Pooling=false }.ToString());
        target.Open(); source.BackupDatabase(target);
    }
}
