namespace GameVault.Core.Services;

public sealed class BackupIntegrityException : Exception
{
    public BackupIntegrityException(string message)
        : base(message)
    {
    }
}
