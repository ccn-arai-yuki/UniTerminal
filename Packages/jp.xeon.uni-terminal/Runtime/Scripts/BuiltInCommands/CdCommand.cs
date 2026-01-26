using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// 作業ディレクトリを変更します
    /// </summary>
    [Command("cd", "Change working directory")]
    public class CdCommand : ICommand
    {
        [Option("logical", "L", Description = "Follow symbolic links (default)")]
        public bool Logical;

        [Option("physical", "P", Description = "Use physical directory structure")]
        public bool Physical;

        public string CommandName => "cd";
        public string Description => "Change working directory";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count > 1)
            {
                await context.Stderr.WriteLineAsync("cd: too many arguments", ct);
                return ExitCode.UsageError;
            }

            if (context.ChangeWorkingDirectory == null)
            {
                await context.Stderr.WriteLineAsync("cd: cannot change directory in this context", ct);
                return ExitCode.RuntimeError;
            }

            var (targetPath, showPath, error) = ResolveTargetPath(context);
            if (error != null)
            {
                await context.Stderr.WriteLineAsync(error, ct);
                return ExitCode.RuntimeError;
            }

            var physicalResult = ConvertToPhysicalPath(targetPath);
            if (physicalResult.error != null)
            {
                await context.Stderr.WriteLineAsync(physicalResult.error, ct);
                return ExitCode.RuntimeError;
            }
            targetPath = physicalResult.path;

            var validationError = ValidateDirectory(targetPath, GetDisplayPath(context, targetPath));
            if (validationError != null)
            {
                await context.Stderr.WriteLineAsync(validationError, ct);
                return ExitCode.RuntimeError;
            }

            context.ChangeWorkingDirectory(targetPath);

            if (showPath)
                await context.Stdout.WriteLineAsync(targetPath, ct);

            return ExitCode.Success;
        }

        private (string path, bool showPath, string error) ResolveTargetPath(CommandContext context)
        {
            if (context.PositionalArguments.Count == 0)
                return (context.HomeDirectory, false, null);

            var arg = context.PositionalArguments[0];

            if (arg == "-")
            {
                if (string.IsNullOrEmpty(context.PreviousWorkingDirectory))
                    return (null, false, "cd: OLDPWD not set");

                return (context.PreviousWorkingDirectory, true, null);
            }

            // ドライブレター指定のチェック（Windows: "C:", "D:" など）
            var driveResolution = TryResolveDrive(arg);
            if (driveResolution.isValid)
            {
                return (driveResolution.path, false, driveResolution.error);
            }

            var resolved = PathUtility.ResolvePath(arg, context.WorkingDirectory, context.HomeDirectory);
            return (resolved, false, null);
        }

        private (bool isValid, string path, string error) TryResolveDrive(string arg)
        {
            // Windowsのドライブレター指定をチェック（例：C: D: など）
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return (false, null, null);

            // ドライブレター形式は X: （大文字小文字問わず）
            if (arg.Length != 2 || arg[1] != ':')
                return (false, null, null);

            char driveLetter = arg[0];
            if (!char.IsLetter(driveLetter))
                return (false, null, null);

            // ドライブパスを構築
            var drivePath = $"{driveLetter}:\\";
            var normalizedPath = PathUtility.NormalizeToSlash(drivePath);

            return (true, normalizedPath, null);
        }

        private (string path, string error) ConvertToPhysicalPath(string targetPath)
        {
            if (!Physical)
                return (targetPath, null);

            try
            {
                var physicalPath = PathUtility.NormalizeToSlash(Path.GetFullPath(targetPath));
                return (physicalPath, null);
            }
            catch (Exception ex)
            {
                return (null, $"cd: {ex.Message}");
            }
        }

        private string ValidateDirectory(string targetPath, string displayPath)
        {
            if (!Directory.Exists(targetPath))
            {
                if (File.Exists(targetPath))
                    return $"cd: {displayPath}: Not a directory";

                return $"cd: {displayPath}: No such file or directory";
            }

            try
            {
                Directory.GetFileSystemEntries(targetPath);
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return $"cd: {displayPath}: Permission denied";
            }
            catch (Exception ex)
            {
                return $"cd: {ex.Message}";
            }
        }

        private string GetDisplayPath(CommandContext context, string targetPath)
        {
            return context.PositionalArguments.Count > 0
                ? context.PositionalArguments[0]
                : targetPath;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // パス補完はCompletionEngineで処理（ディレクトリのみ）
            yield break;
        }
    }
}
