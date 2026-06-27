using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GameVault.Core.Services;

public class SecureBackupPayloadService
{
    private readonly SecureBackupCryptoService _cryptoService;

    public SecureBackupPayloadService()
        : this(new SecureBackupCryptoService())
    {
    }

    public SecureBackupPayloadService(SecureBackupCryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    public string CreateEncryptedBackup(string outputPath, string password, string jsonPayload)
    {
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
            _cryptoService.EncryptFile(temporaryEnvelope, outputPath, password);
            return outputPath;
        }
        finally
        {
            DeleteIfExists(temporaryEnvelope);
        }
    }

    public string ReadVerifiedPayloadJson(string backupPath, string password)
    {
        var envelope = ReadVerifiedEnvelope(backupPath, password);
        return envelope.PayloadJson;
    }

    public SecureBackupValidationResult ValidateBackup(string backupPath, string password)
    {
        try
        {
            ReadVerifiedEnvelope(backupPath, password);
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.Valid,
                "Backup is valid.");
        }
        catch (FileNotFoundException)
        {
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.FileNotFound,
                "Backup file was not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.FileNotFound,
                "Backup file was not found.");
        }
        catch (InvalidBackupFormatException exception)
        {
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.InvalidBackupFormat,
                exception.Message);
        }
        catch (WrongPasswordOrDecryptionFailedException exception)
        {
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.WrongPasswordOrDecryptionFailed,
                exception.Message);
        }
        catch (BackupIntegrityException exception)
        {
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.IntegrityCheckFailed,
                exception.Message);
        }
        catch (Exception exception)
        {
            return new SecureBackupValidationResult(
                SecureBackupValidationStatus.UnexpectedError,
                $"Unexpected error: {exception.Message}");
        }
    }

    private SecureBackupEnvelope ReadVerifiedEnvelope(string backupPath, string password)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException("Backup file was not found.", backupPath);
        }

        var temporaryEnvelope = Path.Combine(
            Path.GetTempPath(),
            $"gamevault-verified-{Guid.NewGuid():N}.json");

        try
        {
            try
            {
                _cryptoService.DecryptFile(backupPath, temporaryEnvelope, password);
            }
            catch (InvalidDataException exception)
            {
                throw new InvalidBackupFormatException(
                    "This is not a valid GameVault secure backup file.",
                    exception);
            }
            catch (CryptographicException exception)
            {
                throw new WrongPasswordOrDecryptionFailedException(
                    "The password is incorrect or the secure backup could not be decrypted.",
                    exception);
            }
            catch (ArgumentException exception)
            {
                throw new WrongPasswordOrDecryptionFailedException(
                    "The password is missing or invalid.",
                    exception);
            }

            SecureBackupEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<SecureBackupEnvelope>(
                    File.ReadAllText(temporaryEnvelope, Encoding.UTF8));
            }
            catch (JsonException exception)
            {
                throw new InvalidBackupFormatException(
                    "Secure backup metadata could not be read.",
                    exception);
            }

            if (envelope is null || string.IsNullOrWhiteSpace(envelope.PayloadJson))
            {
                throw new InvalidBackupFormatException(
                    "Secure backup metadata could not be read.");
            }

            var actualHash = ComputeHash(envelope.PayloadJson);
            if (!actualHash.Equals(
                    envelope.PayloadSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BackupIntegrityException(
                    "Secure backup integrity verification failed.");
            }

            return envelope;
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

    private sealed class SecureBackupEnvelope
    {
        public DateTime CreatedAt { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public string PayloadSha256 { get; set; } = string.Empty;
    }
}
