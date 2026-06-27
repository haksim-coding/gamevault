using GameVault.Core.Services;

return Run(args);

static int Run(string[] args)
{
    if (args.Length != 2
        || !args[0].Equals("verify", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(args[1]))
    {
        Console.Error.WriteLine("Usage: GameVault.BackupTool.exe verify \"<path-to-backup.gvb>\"");
        return 1;
    }

    var backupPath = args[1];
    var password = Console.In.ReadLine() ?? string.Empty;
    var validator = new SecureBackupPayloadService();
    var result = validator.ValidateBackup(backupPath, password);

    Console.Out.WriteLine(result.Status == SecureBackupValidationStatus.Valid
        ? "Backup is valid."
        : result.Message);

    return result.Status switch
    {
        SecureBackupValidationStatus.Valid => 0,
        SecureBackupValidationStatus.FileNotFound => 2,
        SecureBackupValidationStatus.InvalidBackupFormat => 3,
        SecureBackupValidationStatus.WrongPasswordOrDecryptionFailed => 4,
        SecureBackupValidationStatus.IntegrityCheckFailed => 5,
        _ => 6
    };
}
