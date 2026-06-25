using System.IO;
using System.Security.Cryptography;

namespace GameVault.Services;

public class HashService
{
    public bool HasHashFile(string filePath) => File.Exists(filePath + ".sha256");

    public string CreateHashFile(string filePath)
    {
        var hash = ComputeSha256(filePath);
        var hashPath = filePath + ".sha256";
        File.WriteAllText(hashPath, hash);
        return hashPath;
    }

    public bool VerifyHash(string filePath)
    {
        var hashPath = filePath + ".sha256";
        if (!File.Exists(hashPath))
        {
            throw new FileNotFoundException("SHA256 sidecar file was not found.", hashPath);
        }

        var expectedHash = File.ReadAllText(hashPath).Trim();
        var actualHash = ComputeSha256(filePath);
        return expectedHash.Equals(actualHash, StringComparison.OrdinalIgnoreCase);
    }

    public string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
