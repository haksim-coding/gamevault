namespace GameVault.Services;

public enum CatalogSignatureFailure
{
    PublicKeyMissing,
    SignatureMissing,
    InvalidSignature,
    VerificationFailed
}

public sealed class CatalogSignatureException : Exception
{
    public CatalogSignatureException(
        CatalogSignatureFailure failure,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public CatalogSignatureFailure Failure { get; }
}
