using System.Collections.Generic;

namespace Xeon.UniTerminal.Parsing
{
    /// <summary>
    /// トークン化された入力をAST構造にパースします。
    /// </summary>
    public class Parser
    {
        private readonly Tokenizer tokenizer = new Tokenizer();

        /// <summary>
        /// 入力文字列をParsedInput構造にパースします。
        /// </summary>
        /// <param name="input">パースする入力文字列。</param>
        /// <returns>パースされた入力構造。</returns>
        /// <exception cref="ParseException">パースエラー時にスローされます。</exception>
        public ParsedInput Parse(string input)
        {
            var tokens = tokenizer.Tokenize(input);
            return ParseTokens(tokens);
        }

        /// <summary>
        /// トークンをParsedInput構造にパースします。
        /// </summary>
        public ParsedInput ParseTokens(List<Token> tokens)
        {
            var result = new ParsedInput();

            if (tokens == null || tokens.Count == 0)
                return result;

            result.Pipeline = ParsePipeline(tokens);
            return result;
        }

        #region Pipeline Parsing

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
                    ProcessPipeToken(pipeline, commandTokens, lastWasRedirectOut);
                    commandTokens.Clear();
                    lastWasRedirectOut = false;
                }
                else
                {
                    lastWasRedirectOut = UpdateRedirectOutState(token, lastWasRedirectOut);
                    commandTokens.Add(token);
                }
            }

            FinalizeLastCommand(pipeline, commandTokens);
            return pipeline;
        }

        private void ProcessPipeToken(ParsedPipeline pipeline, List<Token> commandTokens, bool lastWasRedirectOut)
        {
            if (lastWasRedirectOut)
                throw new ParseException("Cannot use pipe after stdout redirection (>)");

            if (commandTokens.Count == 0)
                throw new ParseException("Empty command before pipe");

            var command = ParseCommand(commandTokens);

            if (command.Redirections.StdoutMode != RedirectMode.None)
                throw new ParseException("Cannot use pipe after stdout redirection (>)");

            pipeline.Commands.Add(command);
        }

        private static bool UpdateRedirectOutState(Token token, bool lastWasRedirectOut)
        {
            if (token.Kind == TokenKind.RedirectOut || token.Kind == TokenKind.RedirectAppend)
                return true;

            if (token.Kind == TokenKind.Word || token.Kind == TokenKind.EndOfOptions)
                return false;

            return lastWasRedirectOut;
        }

        private void FinalizeLastCommand(ParsedPipeline pipeline, List<Token> commandTokens)
        {
            if (commandTokens.Count == 0)
                throw new ParseException("Empty command after pipe");

            pipeline.Commands.Add(ParseCommand(commandTokens));
        }

        #endregion

        #region Command Parsing

        private ParsedCommand ParseCommand(List<Token> tokens)
        {
            if (tokens.Count == 0)
                throw new ParseException("Empty command");

            var context = new CommandParseContext();

            for (int i = 0; i < tokens.Count; i++)
            {
                ProcessToken(tokens, ref i, context);
            }

            if (context.Command.CommandName == null)
                throw new ParseException("Command name is missing");

            context.Command.Redirections = new ParsedRedirections(
                context.StdinPath, context.StdoutPath, context.StdoutMode);

            return context.Command;
        }

        private void ProcessToken(List<Token> tokens, ref int i, CommandParseContext context)
        {
            var token = tokens[i];

            if (TryProcessRedirection(tokens, ref i, token, context))
                return;

            if (token.Kind == TokenKind.EndOfOptions)
            {
                context.AfterEndOfOptions = true;
                return;
            }

            if (token.Kind == TokenKind.Word)
                ProcessWordToken(tokens, ref i, token, context);
        }

        private bool TryProcessRedirection(List<Token> tokens, ref int i, Token token, CommandParseContext context)
        {
            switch (token.Kind)
            {
                case TokenKind.RedirectIn:
                    context.StdinPath = ParseRedirectionPath(tokens, ref i, "<");
                    return true;

                case TokenKind.RedirectOut:
                    context.StdoutPath = ParseRedirectionPath(tokens, ref i, ">");
                    context.StdoutMode = RedirectMode.Overwrite;
                    return true;

                case TokenKind.RedirectAppend:
                    context.StdoutPath = ParseRedirectionPath(tokens, ref i, ">>");
                    context.StdoutMode = RedirectMode.Append;
                    return true;

                default:
                    return false;
            }
        }

        private static string ParseRedirectionPath(List<Token> tokens, ref int i, string symbol)
        {
            i++;
            if (i >= tokens.Count || tokens[i].Kind != TokenKind.Word)
                throw new ParseException($"Expected file path after {symbol}");

            return tokens[i].Value;
        }

        private void ProcessWordToken(List<Token> tokens, ref int i, Token token, CommandParseContext context)
        {
            // 最初のワードはコマンド名
            if (context.Command.CommandName == null)
            {
                context.Command.CommandName = token.Value;
                return;
            }

            // --の後はすべて位置引数
            if (context.AfterEndOfOptions)
            {
                context.Command.PositionalArguments.Add(token.Value);
                return;
            }

            // オプションまたは位置引数の処理
            if (TryProcessOption(tokens, ref i, token, context))
                return;

            // 通常の位置引数
            context.Command.PositionalArguments.Add(token.Value);
        }

        private bool TryProcessOption(List<Token> tokens, ref int i, Token token, CommandParseContext context)
        {
            if (token.Value.StartsWith("--"))
            {
                ProcessLongOption(tokens, ref i, token, context);
                return true;
            }

            if (token.Value.StartsWith("-") && token.Value.Length > 1 && !IsNumber(token.Value))
            {
                ProcessShortOption(tokens, ref i, token, context);
                return true;
            }

            return false;
        }

        private void ProcessLongOption(List<Token> tokens, ref int i, Token token, CommandParseContext context)
        {
            var opt = ParseLongOption(token.Value, token.WasQuoted);

            // スペース区切りの値をチェック (--name value)
            if (!opt.HasValue && i + 1 < tokens.Count)
            {
                var nextToken = tokens[i + 1];
                if (IsOptionValue(nextToken))
                {
                    opt = new ParsedOptionOccurrence(
                        opt.Name, true, nextToken.Value, true, nextToken.WasQuoted, isValueSpaceSeparated: true);
                    i++;
                }
            }

            context.Command.Options.Add(opt);
        }

        private void ProcessShortOption(List<Token> tokens, ref int i, Token token, CommandParseContext context)
        {
            var opts = ParseShortOptions(token.Value, ref i);

            // 値のない単一のショートオプションの場合、スペース区切りの値をチェック
            if (opts.Count == 1 && !opts[0].HasValue && i < tokens.Count)
            {
                var nextToken = tokens[i];
                if (IsOptionValue(nextToken))
                {
                    opts[0] = new ParsedOptionOccurrence(
                        opts[0].Name, false, nextToken.Value, true, nextToken.WasQuoted, isValueSpaceSeparated: true);
                    i++;
                }
            }

            context.Command.Options.AddRange(opts);
        }

        private static bool IsOptionValue(Token token)
        {
            return token.Kind == TokenKind.Word && !token.Value.StartsWith("-");
        }

        #endregion

        #region Option Parsing

        private ParsedOptionOccurrence ParseLongOption(string value, bool wasQuoted)
        {
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

        private List<ParsedOptionOccurrence> ParseShortOptions(string value, ref int index)
        {
            var withoutDash = value.Substring(1);
            int eqIndex = withoutDash.IndexOf('=');

            index++;

            if (eqIndex >= 0)
                return ParseShortOptionsWithValue(withoutDash, eqIndex);

            return ParseShortOptionsWithoutValue(withoutDash);
        }

        private List<ParsedOptionOccurrence> ParseShortOptionsWithValue(string withoutDash, int eqIndex)
        {
            if (eqIndex == 0)
                throw new ParseException("Invalid option format: -=");

            var results = new List<ParsedOptionOccurrence>();
            var name = withoutDash.Substring(0, eqIndex);
            var rawValue = withoutDash.Substring(eqIndex + 1);

            // -abc=value - 最後のオプションのみが値を取得
            for (int i = 0; i < name.Length - 1; i++)
            {
                results.Add(new ParsedOptionOccurrence(name[i].ToString(), false));
            }
            results.Add(new ParsedOptionOccurrence(name[name.Length - 1].ToString(), false, rawValue, true));

            return results;
        }

        private static List<ParsedOptionOccurrence> ParseShortOptionsWithoutValue(string withoutDash)
        {
            var results = new List<ParsedOptionOccurrence>();

            foreach (char c in withoutDash)
            {
                results.Add(new ParsedOptionOccurrence(c.ToString(), false));
            }

            return results;
        }

        #endregion

        #region Utility

        private static bool IsNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            int start = 0;
            if (value[0] == '-' || value[0] == '+')
            {
                if (value.Length == 1)
                    return false;
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

        #endregion

        #region Parse Context

        private class CommandParseContext
        {
            public ParsedCommand Command { get; } = new ParsedCommand();
            public bool AfterEndOfOptions { get; set; }
            public string StdinPath { get; set; }
            public string StdoutPath { get; set; }
            public RedirectMode StdoutMode { get; set; } = RedirectMode.None;
        }

        #endregion
    }
}
