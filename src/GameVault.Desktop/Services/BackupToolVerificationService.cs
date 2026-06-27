using System.Diagnostics;
using System.IO;
using System.Text;

namespace GameVault.Services;

public class BackupToolVerificationService
{
    private const string ToolFileName = "GameVault.BackupTool.exe";

    public async Task<BackupToolResult> VerifyAsync(string backupPath, string password)
    {
        var toolPath = FindToolPath();
        if (toolPath is null)
        {
            return new BackupToolResult(
                BackupToolExitCode.ToolNotFound,
                string.Empty,
                "Backup verification tool was not found.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("verify");
        startInfo.ArgumentList.Add(backupPath);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();

            await process.StandardInput.WriteLineAsync(password);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new BackupToolResult(
                (BackupToolExitCode)process.ExitCode,
                await stdoutTask,
                await stderrTask);
        }
        catch (Exception exception)
        {
            return new BackupToolResult(
                BackupToolExitCode.UnexpectedError,
                string.Empty,
                exception.Message);
        }
    }

    private static string? FindToolPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, ToolFileName);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var developmentPath = Path.Combine(
                current.FullName,
                "src",
                "GameVault.BackupTool",
                "bin",
                "Debug",
                "net10.0",
                ToolFileName);
            if (File.Exists(developmentPath))
            {
                return developmentPath;
            }

            current = current.Parent;
        }

        return null;
    }
}

public sealed record BackupToolResult(
    BackupToolExitCode ExitCode,
    string StandardOutput,
    string StandardError)
{
    public string CombinedOutput
    {
        get
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(StandardOutput))
            {
                builder.AppendLine(StandardOutput.Trim());
            }

            if (!string.IsNullOrWhiteSpace(StandardError))
            {
                builder.AppendLine(StandardError.Trim());
            }

            return builder.ToString().Trim();
        }
    }
}

public enum BackupToolExitCode
{
    Valid = 0,
    InvalidArguments = 1,
    FileNotFound = 2,
    InvalidBackupFormat = 3,
    WrongPasswordOrDecryptionFailed = 4,
    IntegrityCheckFailed = 5,
    UnexpectedError = 6,
    ToolNotFound = 100
}
