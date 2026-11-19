using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperBoostX.Services
{
    public sealed class PersistedSecureSecrets
    {
        public string DiscordWebhookUrl { get; set; } = "";
        public string DiscordUpdateWebhookUrl { get; set; } = "";
        public string NvidiaApiKey { get; set; } = "";
    }

    public sealed class SecureSecretStoreService
    {
        private const string NvidiaTarget = "HyperBoostX:NVIDIA:ApiKey";
        private const string DiscordTarget = "HyperBoostX:Discord:WebhookUrl";
        private const string DiscordUpdateTarget = "HyperBoostX:Discord:UpdateWebhookUrl";
        private const int CredTypeGeneric = 1;
        private const int CredPersistLocalMachine = 2;

        public SecureSecretStoreService()
        {
        }

        public Task<PersistedSecureSecrets> LoadAsync()
        {
            var secrets = new PersistedSecureSecrets
            {
                NvidiaApiKey = ReadCredential(NvidiaTarget),
                DiscordWebhookUrl = ReadCredential(DiscordTarget),
                DiscordUpdateWebhookUrl = ReadCredential(DiscordUpdateTarget)
            };

            return Task.FromResult(secrets);
        }

        public Task SaveAsync(PersistedSecureSecrets secrets)
        {
            WriteCredential(NvidiaTarget, secrets.NvidiaApiKey);
            WriteCredential(DiscordTarget, secrets.DiscordWebhookUrl);
            WriteCredential(DiscordUpdateTarget, secrets.DiscordUpdateWebhookUrl);
            return Task.CompletedTask;
        }

        public Task ClearDiscordAsync()
        {
            DeleteCredential(DiscordTarget);
            DeleteCredential(DiscordUpdateTarget);
            return Task.CompletedTask;
        }

        public string GetSecretPath()
        {
            return "Windows Credential Manager";
        }

        private static void WriteCredential(string target, string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                DeleteCredential(target);
                return;
            }

            var secretBytes = Encoding.Unicode.GetBytes(secret);
            var credential = new NativeCredential
            {
                Type = CredTypeGeneric,
                TargetName = target,
                CredentialBlobSize = (uint)secretBytes.Length,
                Persist = CredPersistLocalMachine,
                AttributeCount = 0,
                UserName = Environment.UserName
            };

            var blobPtr = Marshal.AllocCoTaskMem(secretBytes.Length);
            try
            {
                Marshal.Copy(secretBytes, 0, blobPtr, secretBytes.Length);
                credential.CredentialBlob = blobPtr;

                if (!CredWrite(ref credential, 0))
                {
                    throw new InvalidOperationException($"Credential Manager write failed for {target}. Error: {Marshal.GetLastWin32Error()}");
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(blobPtr);
            }
        }

        private static string ReadCredential(string target)
        {
            if (!CredRead(target, CredTypeGeneric, 0, out var credPtr) || credPtr == IntPtr.Zero)
                return "";

            try
            {
                var credential = Marshal.PtrToStructure<NativeCredential>(credPtr);
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                    return "";

                var secretBytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, secretBytes, 0, (int)credential.CredentialBlobSize);
                return Encoding.Unicode.GetString(secretBytes).TrimEnd('\0');
            }
            finally
            {
                CredFree(credPtr);
            }
        }

        private static void DeleteCredential(string target)
        {
            CredDelete(target, CredTypeGeneric, 0);
        }

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, int type, int flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr credentialPtr);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }
    }
}
