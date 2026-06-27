using System.IO;
using GameVault.Core.Services;

namespace GameVault.Services;

public class SecureBackupService
{
    private readonly LibraryExportService _exportService;
    private readonly SecureBackupPayloadService _payloadService;

    public SecureBackupService(
        LibraryExportService exportService,
        SecureBackupPayloadService payloadService)
    {
        _exportService = exportService;
        _payloadService = payloadService;
    }

    public string CreateBackup(string outputPath, string password)
    {
        var jsonPayload = _exportService.CreateJson();
        return _payloadService.CreateEncryptedBackup(outputPath, password, jsonPayload);
    }

    public int RestoreBackup(string backupPath, string password)
    {
        try
        {
            var payloadJson = _payloadService.ReadVerifiedPayloadJson(backupPath, password);
            return _exportService.ImportJsonContent(payloadJson);
        }
        catch (WrongPasswordOrDecryptionFailedException exception)
        {
            throw new InvalidDataException(
                "The password is incorrect or the secure backup is damaged.",
                exception);
        }
        catch (BackupIntegrityException exception)
        {
            throw new InvalidDataException(
                "Secure backup integrity verification failed. No games were imported.",
                exception);
        }
    }
}
