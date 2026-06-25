using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GameVault.Services;

public class SecureBackupService
{
    private readonly LibraryExportService _exportService;
    private readonly AesEncryptionService _encryptionService;

    public SecureBackupService(
        LibraryExportService exportService,
        AesEncryptionService encryptionService)
    {
        _exportService = exportService;
        _encryptionService = encryptionService;
    }

    public string CreateBackup(string outputPath, string password)
    {
        var jsonPayload = _exportService.CreateJson();
        var envelope = new SecureBackupEnvelope
        {
            CreatedAt = DateTime.Now,
            PayloadJson = jsonPayload,
            PayloadSha256 = ComputeHash(jsonPayload)
        };

        var temporaryEnvelope = Path.Combine(
            Path.GetTempPath(),
            $"gamevault-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(
                temporaryEnvelope,
                JsonSerializer.Serialize(envelope),
                Encoding.UTF8);
            _encryptionService.EncryptFile(temporaryEnvelope, outputPath, password);
            return outputPath;
        }
        finally
        {
            DeleteIfExists(temporaryEnvelope);
        }
    }

    public int RestoreBackup(string backupPath, string password)
    {
        var temporaryEnvelope = Path.Combine(
            Path.GetTempPath(),
            $"gamevault-restored-{Guid.NewGuid():N}.json");

        try
        {
            try
            {
                _encryptionService.DecryptFile(backupPath, temporaryEnvelope, password);
            }
            catch (CryptographicException exception)
            {
                throw new InvalidDataException(
                    "The password is incorrect or the secure backup is damaged.",
                    exception);
            }

            var envelope = JsonSerializer.Deserialize<SecureBackupEnvelope>(
                               File.ReadAllText(temporaryEnvelope, Encoding.UTF8))
                           ?? throw new InvalidDataException(
                               "Secure backup metadata could not be read.");

            var actualHash = ComputeHash(envelope.PayloadJson);
            if (!actualHash.Equals(
                    envelope.PayloadSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Secure backup integrity verification failed. No games were imported.");
            }

            return _exportService.ImportJsonContent(envelope.PayloadJson);
        }
        finally
        {
            DeleteIfExists(temporaryEnvelope);
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private class SecureBackupEnvelope
    {
        public DateTime CreatedAt { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public string PayloadSha256 { get; set; } = string.Empty;
    }
}
