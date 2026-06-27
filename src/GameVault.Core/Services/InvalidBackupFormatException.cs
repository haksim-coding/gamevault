namespace GameVault.Core.Services;

public sealed class InvalidBackupFormatException : Exception
{
    public InvalidBackupFormatException(string message)
        : base(message)
    {
    }

    public InvalidBackupFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
