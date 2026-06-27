namespace GameVault.Core.Services;

public enum SecureBackupValidationStatus
{
    Valid,
    FileNotFound,
    InvalidBackupFormat,
    WrongPasswordOrDecryptionFailed,
    IntegrityCheckFailed,
    UnexpectedError
}
