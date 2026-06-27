namespace GameVault.Core.Services;

public sealed record SecureBackupValidationResult(
    SecureBackupValidationStatus Status,
    string Message)
{
    public bool IsValid => Status == SecureBackupValidationStatus.Valid;
}
