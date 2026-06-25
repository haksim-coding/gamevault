using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GameVault.Services;

public class AesEncryptionService
{
    private static readonly byte[] Header = Encoding.ASCII.GetBytes("GVAE1");
    private const int Iterations = 100_000;

    public string EncryptFile(string inputPath, string password)
    {
        return EncryptFile(inputPath, inputPath + ".enc", password);
    }

    public string EncryptFile(string inputPath, string outputPath, string password)
    {
        ValidatePassword(password);

        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var key = DeriveKey(password, salt);
        var plaintext = File.ReadAllBytes(inputPath);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        using var output = File.Create(outputPath);
        output.Write(Header);
        output.Write(salt);
        output.Write(nonce);
        output.Write(tag);
        output.Write(ciphertext);
        return outputPath;
    }

    public string DecryptFile(string encryptedPath, string password)
    {
        var basePath = encryptedPath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
            ? encryptedPath[..^4]
            : encryptedPath;
        var outputPath = Path.Combine(
            Path.GetDirectoryName(basePath) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(basePath)}.decrypted{Path.GetExtension(basePath)}");
        return DecryptFile(encryptedPath, outputPath, password);
    }

    public string DecryptFile(string encryptedPath, string outputPath, string password)
    {
        ValidatePassword(password);

        var encrypted = File.ReadAllBytes(encryptedPath);
        var minimumLength = Header.Length + 16 + 12 + 16;
        if (encrypted.Length < minimumLength
            || !encrypted.AsSpan(0, Header.Length).SequenceEqual(Header))
        {
            throw new InvalidDataException("This is not a valid GameVault encrypted file.");
        }

        var offset = Header.Length;
        var salt = encrypted.AsSpan(offset, 16).ToArray();
        offset += 16;
        var nonce = encrypted.AsSpan(offset, 12).ToArray();
        offset += 12;
        var tag = encrypted.AsSpan(offset, 16).ToArray();
        offset += 16;
        var ciphertext = encrypted.AsSpan(offset).ToArray();
        var plaintext = new byte[ciphertext.Length];
        var key = DeriveKey(password, salt);

        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        File.WriteAllBytes(outputPath, plaintext);
        return outputPath;
    }

    private static byte[] DeriveKey(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            32);

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.");
        }
    }
}
