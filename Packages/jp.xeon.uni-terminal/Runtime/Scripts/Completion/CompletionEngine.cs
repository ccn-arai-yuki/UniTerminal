using System;
using System.Collections.Generic;
using System.IO;

namespace Xeon.UniTerminal.Completion
{
    /// <summary>
    /// タブ補完用エンジン。
    /// </summary>
    public class CompletionEngine
    {
        private readonly CommandRegistry registry;
        private readonly string workingDirectory;
        private readonly string homeDirectory;

        public CompletionEngine(CommandRegistry registry, string workingDirectory, string homeDirectory)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            this.homeDirectory = homeDirectory ?? throw new ArgumentNullException(nameof(homeDirectory));
        }

        /// <summary>
        /// 指定された入力に対する補完候補を取得します。
        /// </summary>
        /// <param name="input">現在の入力行。</param>
        /// <returns>候補を含む補完結果。</returns>
        public CompletionResult GetCompletions(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                // コマンド名を補完
                return GetCommandCompletions("", 0);
            }

            // 末尾の現在のトークンを検索
            var (currentToken, tokenStart) = ExtractCurrentToken(input);
            var context = AnalyzeContext(input, tokenStart);

            switch (context.Target)
            {
                case CompletionTarget.CommandName:
                    return GetCommandCompletions(currentToken, tokenStart);

                case CompletionTarget.OptionName:
                    return GetOptionCompletions(currentToken, tokenStart, context.CommandName);

                case CompletionTarget.Path:
                    return GetPathCompletions(currentToken, tokenStart);

                case CompletionTarget.Argument:
                default:
                    // コマンド固有の補完を試し、次にパスを補完
                    var cmdCompletions = GetCommandSpecificCompletions(currentToken, tokenStart, context);
                    if (cmdCompletions.Candidates.Count > 0)
                        return cmdCompletions;
                    return GetPathCompletions(currentToken, tokenStart);
            }
        }

        private (string token, int start) ExtractCurrentToken(string input)
        {
            if (input.Length == 0)
                return ("", 0);

            // スペースで終わる場合、新しいトークンを開始
            if (input[input.Length - 1] == ' ')
                return ("", input.Length);

            // 現在のトークンの開始位置を検索（シンプルなアプローチ）
            int start = input.Length - 1;
            while (start > 0 && input[start - 1] != ' ')
            {
                start--;
            }

            return (input.Substring(start), start);
        }

        private CompletionAnalysis AnalyzeContext(string input, int tokenStart)
        {
            var analysis = new CompletionAnalysis();

            // 現在のトークンより前の部分を検索
            var beforeToken = input.Substring(0, tokenStart).TrimEnd();
            var parts = beforeToken.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                // 最初のトークン - コマンド名
                analysis.Target = CompletionTarget.CommandName;
                return analysis;
            }

            // 現在のコマンドコンテキストを決定するために最後のパイプを検索
            int lastPipe = -1;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i] == "|")
                {
                    lastPipe = i;
                    break;
                }
            }

            // 現在のセグメントのコマンド名を取得
            int cmdStart = lastPipe + 1;
            if (cmdStart < parts.Length)
            {
                analysis.CommandName = parts[cmdStart];
            }
            else
            {
                // パイプの後、コマンド名が必要
                analysis.Target = CompletionTarget.CommandName;
                return analysis;
            }

            // 何を補完しているかをチェック
            var currentToken = tokenStart < input.Length ? input.Substring(tokenStart) : "";

            // <または>の後、パスを補完
            if (parts.Length > 0)
            {
                var lastPart = parts[parts.Length - 1];
                if (lastPart == "<" || lastPart == ">" || lastPart == ">>")
                {
                    analysis.Target = CompletionTarget.Path;
                    return analysis;
                }
            }

            // 現在のトークンが-で始まる場合、オプションを補完
            if (currentToken.StartsWith("-"))
            {
                analysis.Target = CompletionTarget.OptionName;
                return analysis;
            }

            // それ以外の場合、引数
            analysis.Target = CompletionTarget.Argument;
            analysis.TokenIndex = parts.Length - cmdStart;
            return analysis;
        }

        private CompletionResult GetCommandCompletions(string prefix, int tokenStart)
        {
            var candidates = new List<CompletionCandidate>();

            foreach (var name in registry.GetCommandNames())
            {
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!registry.TryGetCommand(name, out var meta))
                    continue;
                candidates.Add(new CompletionCandidate(name, $"{name} - {meta.Description}", CompletionTarget.CommandName));
            }

            // アルファベット順にソート
            candidates.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));

            return new CompletionResult(candidates, tokenStart, prefix.Length);
        }

        private CompletionResult GetOptionCompletions(string prefix, int tokenStart, string commandName)
        {
            var candidates = new List<CompletionCandidate>();

            if (string.IsNullOrEmpty(commandName) || !registry.TryGetCommand(commandName, out var metadata))
            {
                return new CompletionResult(candidates, tokenStart, prefix.Length);
            }

            foreach (var opt in metadata.Options)
            {
                // ロングオプション
                var longOpt = $"--{opt.LongName}";
                if (longOpt.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(new CompletionCandidate(
                        longOpt,
                        $"{longOpt} - {opt.Description}",
                        CompletionTarget.OptionName));
                }

                // ショートオプション
                if (string.IsNullOrEmpty(opt.ShortName))
                    continue;
                var shortOpt = $"-{opt.ShortName}";
                if (!shortOpt.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                candidates.Add(new CompletionCandidate(
                    shortOpt,
                    $"{shortOpt} (--{opt.LongName}) - {opt.Description}",
                    CompletionTarget.OptionName));
            }

            // アルファベット順にソート
            candidates.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));

            return new CompletionResult(candidates, tokenStart, prefix.Length);
        }

        private CompletionResult GetPathCompletions(string prefix, int tokenStart)
        {
            var candidates = new List<CompletionCandidate>();

            try
            {
                // パスを解決
                string basePath;
                string searchPattern;

                if (string.IsNullOrEmpty(prefix))
                {
                    basePath = workingDirectory;
                    searchPattern = "";
                }
                else if (prefix.StartsWith("~"))
                {
                    // ホームディレクトリ展開
                    var expandedPath = prefix.Length == 1
                        ? homeDirectory
                        : PathUtility.Combine(homeDirectory, prefix.Substring(2));

                    if (Directory.Exists(expandedPath))
                    {
                        basePath = expandedPath;
                        searchPattern = "";
                    }
                    else
                    {
                        basePath = PathUtility.GetDirectoryName(expandedPath) ?? homeDirectory;
                        searchPattern = Path.GetFileName(expandedPath);
                    }
                }
                else if (Path.IsPathRooted(prefix))
                {
                    if (Directory.Exists(prefix))
                    {
                        basePath = prefix;
                        searchPattern = "";
                    }
                    else
                    {
                        basePath = PathUtility.GetDirectoryName(prefix) ?? workingDirectory;
                        searchPattern = Path.GetFileName(prefix);
                    }
                }
                else
                {
                    var fullPath = PathUtility.Combine(workingDirectory, prefix);
                    if (Directory.Exists(fullPath))
                    {
                        basePath = fullPath;
                        searchPattern = "";
                    }
                    else
                    {
                        basePath = PathUtility.GetDirectoryName(fullPath);
                        if (string.IsNullOrEmpty(basePath))
                            basePath = workingDirectory;
                        searchPattern = Path.GetFileName(prefix);
                    }
                }

                if (!Directory.Exists(basePath))
                {
                    return new CompletionResult(candidates, tokenStart, prefix.Length);
                }

                // まずディレクトリ、次にファイルをリスト
                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    var name = Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(searchPattern) &&
                        !name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var completionPath = GetCompletionPath(prefix, basePath, name, true);
                    candidates.Add(new CompletionCandidate(
                        completionPath,
                        $"{name}/",
                        CompletionTarget.Path));
                }

                foreach (var file in Directory.GetFiles(basePath))
                {
                    var name = Path.GetFileName(file);
                    if (!string.IsNullOrEmpty(searchPattern) &&
                        !name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var completionPath = GetCompletionPath(prefix, basePath, name, false);
                    candidates.Add(new CompletionCandidate(
                        completionPath,
                        name,
                        CompletionTarget.Path));
                }
            }
            catch
            {
                // ファイルシステムエラーを無視
            }

            return new CompletionResult(candidates, tokenStart, prefix.Length);
        }

        private string GetCompletionPath(string prefix, string basePath, string name, bool isDirectory)
        {
            string result;

            if (string.IsNullOrEmpty(prefix))
            {
                result = name;
            }
            else if (prefix.StartsWith("~"))
            {
                var normalizedBasePath = PathUtility.NormalizeToSlash(basePath);
                var normalizedHome = PathUtility.NormalizeToSlash(homeDirectory);
                var relativeTohome = normalizedBasePath.StartsWith(normalizedHome)
                    ? normalizedBasePath.Substring(normalizedHome.Length).TrimStart('/')
                    : "";
                result = string.IsNullOrEmpty(relativeTohome)
                    ? $"~/{name}"
                    : $"~/{relativeTohome}/{name}";
            }
            else if (Path.IsPathRooted(prefix))
            {
                result = PathUtility.Combine(basePath, name);
            }
            else
            {
                var prefixDir = PathUtility.GetDirectoryName(prefix);
                result = string.IsNullOrEmpty(prefixDir)
                    ? name
                    : $"{prefixDir}/{name}";
            }

            if (isDirectory && !result.EndsWith("/"))
            {
                result += "/";
            }

            return result;
        }

        private CompletionResult GetCommandSpecificCompletions(string prefix, int tokenStart, CompletionAnalysis analysis)
        {
            if (string.IsNullOrEmpty(analysis.CommandName) ||
                !registry.TryGetCommand(analysis.CommandName, out var metadata))
            {
                return new CompletionResult(new List<CompletionCandidate>(), tokenStart, prefix.Length);
            }

            // コマンド固有の補完を取得
            var context = new CompletionContext(
                "",  // full input not needed here
                prefix,
                analysis.TokenIndex,
                workingDirectory,
                homeDirectory);

            var candidates = new List<CompletionCandidate>();
            try
            {
                var instance = metadata.CreateInstance();
                foreach (var completion in instance.GetCompletions(context))
                {
                    if (completion.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(new CompletionCandidate(completion, completion, CompletionTarget.Argument));
                    }
                }
            }
            catch
            {
                // コマンド補完からのエラーを無視
            }

            return new CompletionResult(candidates, tokenStart, prefix.Length);
        }
    }
}
