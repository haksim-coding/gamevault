using System.Security.Cryptography;

namespace GameVault.ManifestSigner;

internal static class Program
{
    private const int Success = 0;
    private const int InvalidArguments = 1;
    private const int FileNotFound = 2;
    private const int InvalidKey = 3;
    private const int SignatureInvalid = 4;
    private const int UnexpectedError = 5;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                return Fail(InvalidArguments, Usage);
            }

            var options = ParseOptions(args.Skip(1).ToArray());
            return args[0].ToLowerInvariant() switch
            {
                "generate-keys" => GenerateKeys(options),
                "sign" => Sign(options),
                "verify" => Verify(options),
                _ => Fail(InvalidArguments, Usage)
            };
        }
        catch (ArgumentException exception)
        {
            return Fail(InvalidArguments, exception.Message);
        }
        catch (FileNotFoundException exception)
        {
            return Fail(FileNotFound, exception.Message);
        }
        catch (CryptographicException exception)
        {
            return Fail(InvalidKey, $"The RSA key is invalid: {exception.Message}");
        }
        catch (Exception exception)
        {
            return Fail(UnexpectedError, $"Unexpected error: {exception.Message}");
        }
    }

    private static int GenerateKeys(IReadOnlyDictionary<string, string> options)
    {
        var privatePath = Required(options, "private");
        var publicPath = Required(options, "public");
        EnsureParentFolder(privatePath);
        EnsureParentFolder(publicPath);

        using var rsa = RSA.Create(3072);
        File.WriteAllText(privatePath, rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(publicPath, rsa.ExportSubjectPublicKeyInfoPem());
        Console.WriteLine("RSA key pair generated.");
        return Success;
    }

    private static int Sign(IReadOnlyDictionary<string, string> options)
    {
        var manifestPath = ExistingFile(options, "manifest");
        var privatePath = ExistingFile(options, "private");
        var signaturePath = Required(options, "signature");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(privatePath));
        var signature = rsa.SignData(
            File.ReadAllBytes(manifestPath),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        EnsureParentFolder(signaturePath);
        File.WriteAllBytes(signaturePath, signature);
        Console.WriteLine("Manifest signed successfully.");
        return Success;
    }

    private static int Verify(IReadOnlyDictionary<string, string> options)
    {
        var manifestPath = ExistingFile(options, "manifest");
        var publicPath = ExistingFile(options, "public");
        var signaturePath = ExistingFile(options, "signature");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(publicPath));
        var isValid = rsa.VerifyData(
            File.ReadAllBytes(manifestPath),
            File.ReadAllBytes(signaturePath),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        if (!isValid)
        {
            return Fail(SignatureInvalid, "Manifest signature is invalid.");
        }

        Console.WriteLine("Manifest signature is valid.");
        return Success;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        if (args.Length % 2 != 0)
        {
            throw new ArgumentException(Usage);
        }

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(args[index + 1]))
            {
                throw new ArgumentException(Usage);
            }

            options[args[index][2..]] = args[index + 1];
        }

        return options;
    }

    private static string Required(IReadOnlyDictionary<string, string> options, string name) =>
        options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? Path.GetFullPath(value)
            : throw new ArgumentException($"Missing required option --{name}.\n{Usage}");

    private static string ExistingFile(IReadOnlyDictionary<string, string> options, string name)
    {
        var path = Required(options, name);
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException($"File was not found: {path}", path);
    }

    private static void EnsureParentFolder(string path)
    {
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    private static int Fail(int exitCode, string message)
    {
        Console.Error.WriteLine(message);
        return exitCode;
    }

    private const string Usage =
        """
        Usage:
          GameVault.ManifestSigner generate-keys --private private_key.pem --public public_key.pem
          GameVault.ManifestSigner sign --manifest downloads.json --private private_key.pem --signature downloads.json.sig
          GameVault.ManifestSigner verify --manifest downloads.json --public public_key.pem --signature downloads.json.sig
        """;
}
