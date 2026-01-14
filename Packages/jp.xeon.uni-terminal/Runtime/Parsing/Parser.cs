using System.Collections.Generic;

namespace Xeon.UniTerminal.Parsing
{
    /// <summary>
    /// トークン化された入力をAST構造にパースします。
    /// </summary>
    public class Parser
    {
        private readonly Tokenizer _tokenizer = new Tokenizer();

        /// <summary>
        /// 入力文字列をParsedInput構造にパースします。
        /// </summary>
        /// <param name="input">パースする入力文字列。</param>
        /// <returns>パースされた入力構造。</returns>
        /// <exception cref="ParseException">パースエラー時にスローされます。</exception>
        public ParsedInput Parse(string input)
        {
            var tokens = _tokenizer.Tokenize(input);
            return ParseTokens(tokens);
        }

        /// <summary>
        /// トークンをParsedInput構造にパースします。
        /// </summary>
        public ParsedInput ParseTokens(List<Token> tokens)
        {
            var result = new ParsedInput();

            if (tokens == null || tokens.Count == 0)
            {
                return result;
            }

            result.Pipeline = ParsePipeline(tokens);
            return result;
        }

        private ParsedPipeline ParsePipeline(List<Token> tokens)
        {
            var pipeline = new ParsedPipeline();
            var commandTokens = new List<Token>();
            bool lastWasRedirectOut = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.Kind == TokenKind.Pipe)
                {
                    // 出力リダイレクトの後にパイプがある場合はエラー
                    if (lastWasRedirectOut)
                    {
                        throw new ParseException("Cannot use pipe after stdout redirection (>)");
                    }

                    // 現在のコマンドを終了
                    if (commandTokens.Count == 0)
                    {
                        throw new ParseException("Empty command before pipe");
                    }

                    var command = ParseCommand(commandTokens);

                    // このコマンドに標準出力リダイレクトがあるかチェック
                    if (command.Redirections.StdoutMode != RedirectMode.None)
                    {
                        throw new ParseException("Cannot use pipe after stdout redirection (>)");
                    }

                    pipeline.Commands.Add(command);
                    commandTokens.Clear();
                    lastWasRedirectOut = false;
                }
                else
                {
                    if (token.Kind == TokenKind.RedirectOut || token.Kind == TokenKind.RedirectAppend)
                    {
                        lastWasRedirectOut = true;
                    }
                    else if (token.Kind == TokenKind.Word || token.Kind == TokenKind.EndOfOptions)
                    {
                        lastWasRedirectOut = false;
                    }

                    commandTokens.Add(token);
                }
            }

            // 残りのトークンを処理
            if (commandTokens.Count == 0)
            {
                throw new ParseException("Empty command after pipe");
            }

            pipeline.Commands.Add(ParseCommand(commandTokens));

            return pipeline;
        }

        private ParsedCommand ParseCommand(List<Token> tokens)
        {
            var command = new ParsedCommand();
            bool afterEndOfOptions = false;
            int i = 0;

            // 最初のトークンはコマンド名であるべき
            if (tokens.Count == 0)
            {
                throw new ParseException("Empty command");
            }

            while (i < tokens.Count)
            {
                var token = tokens[i];

                // リダイレクションの処理
                if (token.Kind == TokenKind.RedirectIn)
                {
                    i++;
                    if (i >= tokens.Count || tokens[i].Kind != TokenKind.Word)
                    {
                        throw new ParseException("Expected file path after <");
                    }
                    command.Redirections.StdinPath = tokens[i].Value;
                    i++;
                    continue;
                }

                if (token.Kind == TokenKind.RedirectOut)
                {
                    i++;
                    if (i >= tokens.Count || tokens[i].Kind != TokenKind.Word)
                    {
                        throw new ParseException("Expected file path after >");
                    }
                    command.Redirections.StdoutPath = tokens[i].Value;
                    command.Redirections.StdoutMode = RedirectMode.Overwrite;
                    i++;
                    continue;
                }

                if (token.Kind == TokenKind.RedirectAppend)
                {
                    i++;
                    if (i >= tokens.Count || tokens[i].Kind != TokenKind.Word)
                    {
                        throw new ParseException("Expected file path after >>");
                    }
                    command.Redirections.StdoutPath = tokens[i].Value;
                    command.Redirections.StdoutMode = RedirectMode.Append;
                    i++;
                    continue;
                }

                // オプション終端の処理
                if (token.Kind == TokenKind.EndOfOptions)
                {
                    afterEndOfOptions = true;
                    i++;
                    continue;
                }

                // ワードトークンの処理
                if (token.Kind == TokenKind.Word)
                {
                    // 最初のワードはコマンド名
                    if (command.CommandName == null)
                    {
                        command.CommandName = token.Value;
                        i++;
                        continue;
                    }

                    // --の後はすべて位置引数
                    if (afterEndOfOptions)
                    {
                        command.PositionalArguments.Add(token.Value);
                        i++;
                        continue;
                    }

                    // オプションかどうかをチェック
                    if (token.Value.StartsWith("--"))
                    {
                        var opt = ParseLongOption(token.Value, token.WasQuoted);

                        // スペース区切りの値をチェック (--name value)
                        if (!opt.HasValue && i + 1 < tokens.Count)
                        {
                            var nextToken = tokens[i + 1];
                            // 次のトークンがワードでオプションのように見えない場合、値として使用
                            if (nextToken.Kind == TokenKind.Word &&
                                !nextToken.Value.StartsWith("-"))
                            {
                                opt = new ParsedOptionOccurrence(
                                    opt.Name, true, nextToken.Value, true, nextToken.WasQuoted, isValueSpaceSeparated: true);
                                i++; // Skip the value token
                            }
                        }

                        command.Options.Add(opt);
                        i++;
                        continue;
                    }

                    if (token.Value.StartsWith("-") && token.Value.Length > 1 && !IsNumber(token.Value))
                    {
                        // ショートオプション
                        var opts = ParseShortOptions(token.Value, tokens, ref i);

                        // 値のない単一のショートオプションの場合、スペース区切りの値をチェック
                        if (opts.Count == 1 && !opts[0].HasValue && i < tokens.Count)
                        {
                            var nextToken = tokens[i];
                            if (nextToken.Kind == TokenKind.Word &&
                                !nextToken.Value.StartsWith("-"))
                            {
                                opts[0] = new ParsedOptionOccurrence(
                                    opts[0].Name, false, nextToken.Value, true, nextToken.WasQuoted, isValueSpaceSeparated: true);
                                i++; // 値トークンをスキップ
                            }
                        }

                        command.Options.AddRange(opts);
                        continue;
                    }

                    // 通常の位置引数
                    command.PositionalArguments.Add(token.Value);
                    i++;
                    continue;
                }

                i++;
            }

            if (command.CommandName == null)
            {
                throw new ParseException("Command name is missing");
            }

            return command;
        }

        private bool IsNumber(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            int start = 0;
            if (value[0] == '-' || value[0] == '+')
            {
                if (value.Length == 1) return false;
                start = 1;
            }
            for (int i = start; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsDigit(c) && c != '.')
                    return false;
            }
            return true;
        }

        private ParsedOptionOccurrence ParseLongOption(string value, bool wasQuoted)
        {
            // --name または --name=value
            var withoutDashes = value.Substring(2);

            int eqIndex = withoutDashes.IndexOf('=');
            if (eqIndex >= 0)
            {
                var name = withoutDashes.Substring(0, eqIndex);
                var rawValue = withoutDashes.Substring(eqIndex + 1);
                return new ParsedOptionOccurrence(name, true, rawValue, true, wasQuoted);
            }

            return new ParsedOptionOccurrence(withoutDashes, true);
        }

        private List<ParsedOptionOccurrence> ParseShortOptions(string value, List<Token> tokens, ref int index)
        {
            var results = new List<ParsedOptionOccurrence>();
            var withoutDash = value.Substring(1);

            // オプション内の=をチェック
            int eqIndex = withoutDash.IndexOf('=');
            if (eqIndex >= 0)
            {
                // -x=value 形式
                if (eqIndex == 0)
                {
                    throw new ParseException("Invalid option format: -=");
                }

                var name = withoutDash.Substring(0, eqIndex);
                var rawValue = withoutDash.Substring(eqIndex + 1);

                if (name.Length > 1)
                {
                    // -abc=value - 最後のオプションのみが値を取得
                    for (int i = 0; i < name.Length - 1; i++)
                    {
                        results.Add(new ParsedOptionOccurrence(name[i].ToString(), false));
                    }
                    results.Add(new ParsedOptionOccurrence(name[name.Length - 1].ToString(), false, rawValue, true));
                }
                else
                {
                    results.Add(new ParsedOptionOccurrence(name, false, rawValue, true));
                }
                index++;
                return results;
            }

            // =記号なし - 結合されたboolオプションまたはスペース区切りの値を持つ単一オプションの可能性
            // ここでは型が不明なため、各文字を個別のオプションとして扱う
            // バインディング層が値の割り当てを処理する
            foreach (char c in withoutDash)
            {
                results.Add(new ParsedOptionOccurrence(c.ToString(), false));
            }

            index++;
            return results;
        }
    }
}
