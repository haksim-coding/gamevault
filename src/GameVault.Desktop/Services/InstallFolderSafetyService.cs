using System.IO;
using GameVault.Localization;

namespace GameVault.Services;

public static class InstallFolderSafetyService
{
    public static bool IsSafeToDeleteInstallFolder(
        string installPath,
        string defaultInstallFolder,
        out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrWhiteSpace(installPath) || string.IsNullOrWhiteSpace(defaultInstallFolder))
        {
            reason = LocalizationService.Instance.Get("InstallPathEmpty");
            return false;
        }

        try
        {
            var installFullPath = Normalize(installPath);
            var defaultFullPath = Normalize(defaultInstallFolder);
            var documentsPath = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            var driveRoot = Path.GetPathRoot(installFullPath);

            if (!Directory.Exists(installFullPath))
            {
                reason = LocalizationService.Instance.Get("InstallFolderMissing");
                return false;
            }

            if (installFullPath.Equals(defaultFullPath, StringComparison.OrdinalIgnoreCase))
            {
                reason = LocalizationService.Instance.Get("InstallRootProtected");
                return false;
            }

            if ((!string.IsNullOrWhiteSpace(driveRoot)
                 && installFullPath.Equals(Normalize(driveRoot), StringComparison.OrdinalIgnoreCase))
                || installFullPath.Equals(documentsPath, StringComparison.OrdinalIgnoreCase))
            {
                reason = LocalizationService.Instance.Get("ProtectedRoot");
                return false;
            }

            var relativePath = Path.GetRelativePath(defaultFullPath, installFullPath);
            if (Path.IsPathRooted(relativePath)
                || relativePath.Equals("..", StringComparison.Ordinal)
                || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                reason = LocalizationService.Instance.Get("InstallPathOutside");
                return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            reason = LocalizationService.Instance.Get("InvalidInstallPath");
            return false;
        }
    }

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
