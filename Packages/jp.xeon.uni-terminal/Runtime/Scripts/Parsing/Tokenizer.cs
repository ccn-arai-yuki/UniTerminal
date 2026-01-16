using System.Collections.Generic;
using System.Text;

namespace Xeon.UniTerminal.Parsing
{
    /// <summary>
    /// CLI入力をトークンに分割します。
    /// </summary>
    public class Tokenizer
    {
        /// <summary>
        /// 入力文字列をトークンのリストに分割します。
        /// </summary>
        /// <param name="input">トークン化する入力文字列。</param>
        /// <returns>トークンのリスト。</returns>
        /// <exception cref="ParseException">トークン化エラー時にスローされます。</exception>
        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrEmpty(input))
                return tokens;

            int i = 0;
            int length = input.Length;

            while (i < length)
            {
                char c = input[i];

                // スペースをスキップ
                if (c == ' ')
                {
                    i++;
                    continue;
                }

                // タブはエラー
                if (c == '\t')
                {
                    throw new ParseException($"Tab character is not allowed in input at position {i}");
                }

                // 演算子のチェック
                if (c == '|')
                {
                    tokens.Add(new Token(TokenKind.Pipe, "|", new SourceSpan(i, 1)));
                    i++;
                    continue;
                }

                if (c == '<')
                {
                    tokens.Add(new Token(TokenKind.RedirectIn, "<", new SourceSpan(i, 1)));
                    i++;
                    continue;
                }

                if (c == '>')
                {
                    // >>のチェック
                    if (i + 1 < length && input[i + 1] == '>')
                    {
                        tokens.Add(new Token(TokenKind.RedirectAppend, ">>", new SourceSpan(i, 2)));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenKind.RedirectOut, ">", new SourceSpan(i, 1)));
                        i++;
                    }
                    continue;
                }

                // ワードトークンをパース
                var (token, newIndex) = ParseWord(input, i);
                tokens.Add(token);
                i = newIndex;
            }

            return tokens;
        }

        private (Token token, int newIndex) ParseWord(string input, int start)
        {
            var builder = new StringBuilder();
            int i = start;
            int length = input.Length;
            bool wasQuoted = false;

            while (i < length)
            {
                char c = input[i];

                // スペースまたは演算子でワード終了
                if (c == ' ' || c == '|' || c == '<' || c == '>')
                {
                    break;
                }

                // タブはエラー
                if (c == '\t')
                {
                    throw new ParseException($"Tab character is not allowed in input at position {i}");
                }

                // クォート外のエスケープ処理
                if (c == '\\')
                {
                    if (i + 1 >= length)
                    {
                        throw new ParseException($"Escape character at end of input at position {i}");
                    }
                    // 次の文字をエスケープ
                    builder.Append(input[i + 1]);
                    i += 2;
                    continue;
                }

                // ダブルクォートの処理
                if (c == '"')
                {
                    wasQuoted = true;
                    i++;
                    i = ParseDoubleQuoted(input, i, builder);
                    continue;
                }

                // シングルクォートの処理
                if (c == '\'')
                {
                    wasQuoted = true;
                    i++;
                    i = ParseSingleQuoted(input, i, builder);
                    continue;
                }

                // 通常の文字
                builder.Append(c);
                i++;
            }

            string value = builder.ToString();
            var span = new SourceSpan(start, i - start);

            // オプション終端マーカーかどうかをチェック
            if (value == "--" && !wasQuoted)
            {
                return (new Token(TokenKind.EndOfOptions, "--", span), i);
            }

            return (new Token(TokenKind.Word, value, span, wasQuoted), i);
        }

        private int ParseDoubleQuoted(string input, int start, StringBuilder builder)
        {
            int i = start;
            int length = input.Length;

            while (i < length)
            {
                char c = input[i];

                if (c == '"')
                {
                    // ダブルクォート文字列の終了
                    return i + 1;
                }

                if (c == '\\')
                {
                    // ダブルクォート内で有効なエスケープは\"と\\のみ
                    if (i + 1 < length)
                    {
                        char next = input[i + 1];
                        if (next == '"' || next == '\\')
                        {
                            builder.Append(next);
                            i += 2;
                            continue;
                        }
                    }
                    // それ以外の場合、バックスラッシュはリテラル
                    builder.Append(c);
                    i++;
                    continue;
                }

                builder.Append(c);
                i++;
            }

            throw new ParseException($"Unclosed double quote starting at position {start - 1}");
        }

        private int ParseSingleQuoted(string input, int start, StringBuilder builder)
        {
            int i = start;
            int length = input.Length;

            while (i < length)
            {
                char c = input[i];

                if (c == '\'')
                {
                    // シングルクォート文字列の終了
                    return i + 1;
                }

                // シングルクォート内ではエスケープなし - すべてリテラル
                builder.Append(c);
                i++;
            }

            throw new ParseException($"Unclosed single quote starting at position {start - 1}");
        }
    }
}
