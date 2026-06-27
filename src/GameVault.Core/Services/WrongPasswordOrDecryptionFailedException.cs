namespace GameVault.Core.Services;

public sealed class WrongPasswordOrDecryptionFailedException : Exception
{
    public WrongPasswordOrDecryptionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
