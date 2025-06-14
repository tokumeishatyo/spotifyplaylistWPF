using System.Runtime.InteropServices;
using System.Text;

namespace SpotifyManager.Auth.Services;

public class CredentialService
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL userCredential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint reservedFlag);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree([In] IntPtr buffer);

    private const uint CRED_TYPE_GENERIC = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    public async Task<bool> SaveCredentialAsync(string target, string value)
    {
        return await Task.Run(() =>
        {
            try
            {
                var credential = new CREDENTIAL();
                credential.TargetName = Marshal.StringToCoTaskMemUni(target);
                credential.Type = CRED_TYPE_GENERIC;
                credential.UserName = Marshal.StringToCoTaskMemUni("SpotifyManager");
                credential.CredentialBlob = Marshal.StringToCoTaskMemUni(value);
                credential.CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(value);
                credential.Persist = CRED_PERSIST_LOCAL_MACHINE;

                bool result = CredWrite(ref credential, 0);

                Marshal.FreeCoTaskMem(credential.TargetName);
                Marshal.FreeCoTaskMem(credential.UserName);
                Marshal.FreeCoTaskMem(credential.CredentialBlob);

                return result;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<string?> GetCredentialAsync(string target)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (CredRead(target, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
                {
                    var credential = (CREDENTIAL)Marshal.PtrToStructure(credPtr, typeof(CREDENTIAL))!;
                    string value = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2) ?? string.Empty;
                    CredFree(credPtr);
                    return value;
                }
                return null;
            }
            catch
            {
                return null;
            }
        });
    }

    public async Task<bool> DeleteCredentialAsync(string target)
    {
        return await Task.Run(() =>
        {
            try
            {
                return CredDelete(target, CRED_TYPE_GENERIC, 0);
            }
            catch
            {
                return false;
            }
        });
    }
}