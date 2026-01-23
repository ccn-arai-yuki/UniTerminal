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

            var context = new TokenizeContext(input);

            while (context.HasMore)
            {
                SkipSpaces(context);

                if (!context.HasMore)
                    break;

                tokens.Add(ReadNextToken(context));
            }

            return tokens;
        }

        #region Token Reading

        private static void SkipSpaces(TokenizeContext context)
        {
            while (context.HasMore && context.Current == ' ')
            {
                context.Advance();
            }
        }

        private Token ReadNextToken(TokenizeContext context)
        {
            char c = context.Current;

            if (c == '\t')
                throw new ParseException($"Tab character is not allowed in input at position {context.Position}");

            if (IsOperatorChar(c))
                return ReadOperator(context);

            return ReadWord(context);
        }

        private static bool IsOperatorChar(char c)
        {
            return c == '|' || c == '<' || c == '>';
        }

        private Token ReadOperator(TokenizeContext context)
        {
            char c = context.Current;

            switch (c)
            {
                case '|':
                    context.Advance();
                    return CreateToken(TokenKind.Pipe, "|", context.Position - 1, 1);

                case '<':
                    context.Advance();
                    return CreateToken(TokenKind.RedirectIn, "<", context.Position - 1, 1);

                case '>':
                    return ReadRedirectOut(context);

                default:
                    throw new ParseException($"Unexpected operator character: {c}");
            }
        }

        private Token ReadRedirectOut(TokenizeContext context)
        {
            int start = context.Position;

            if (context.PeekNext() == '>')
            {
                context.Advance(2);
                return CreateToken(TokenKind.RedirectAppend, ">>", start, 2);
            }

            context.Advance();
            return CreateToken(TokenKind.RedirectOut, ">", start, 1);
        }

        private static Token CreateToken(TokenKind kind, string value, int start, int length)
        {
            return new Token(kind, value, new SourceSpan(start, length));
        }

        #endregion

        #region Word Parsing

        private Token ReadWord(TokenizeContext context)
        {
            var builder = new StringBuilder();
            int start = context.Position;
            bool wasQuoted = false;

            while (context.HasMore && !IsWordTerminator(context.Current))
            {
                ProcessWordCharacter(context, builder, ref wasQuoted);
            }

            return CreateWordToken(builder.ToString(), start, context.Position, wasQuoted);
        }

        private void ProcessWordCharacter(TokenizeContext context, StringBuilder builder, ref bool wasQuoted)
        {
            char c = context.Current;

            if (c == '\t')
                throw new ParseException($"Tab character is not allowed in input at position {context.Position}");

            if (c == '\\')
            {
                ProcessEscape(context, builder);
                return;
            }

            if (c == '"')
            {
                wasQuoted = true;
                context.Advance();
                ReadDoubleQuotedContent(context, builder);
                return;
            }

            if (c == '\'')
            {
                wasQuoted = true;
                context.Advance();
                ReadSingleQuotedContent(context, builder);
                return;
            }

            builder.Append(c);
            context.Advance();
        }

        private static bool IsWordTerminator(char c)
        {
            return c == ' ' || c == '|' || c == '<' || c == '>';
        }

        private static Token CreateWordToken(string value, int start, int end, bool wasQuoted)
        {
            var span = new SourceSpan(start, end - start);

            if (value == "--" && !wasQuoted)
                return new Token(TokenKind.EndOfOptions, "--", span);

            return new Token(TokenKind.Word, value, span, wasQuoted);
        }

        #endregion

        #region Escape Processing

        private static void ProcessEscape(TokenizeContext context, StringBuilder builder)
        {
            if (!context.HasNext)
                throw new ParseException($"Escape character at end of input at position {context.Position}");

            context.Advance();
            builder.Append(context.Current);
            context.Advance();
        }

        #endregion

        #region Quote Processing

        private void ReadDoubleQuotedContent(TokenizeContext context, StringBuilder builder)
        {
            int quoteStart = context.Position - 1;

            while (context.HasMore)
            {
                char c = context.Current;

                if (c == '"')
                {
                    context.Advance();
                    return;
                }

                if (c == '\\')
                {
                    ProcessDoubleQuoteEscape(context, builder);
                    continue;
                }

                builder.Append(c);
                context.Advance();
            }

            throw new ParseException($"Unclosed double quote starting at position {quoteStart}");
        }

        private static void ProcessDoubleQuoteEscape(TokenizeContext context, StringBuilder builder)
        {
            if (context.HasNext)
            {
                char next = context.PeekNext();
                if (next == '"' || next == '\\')
                {
                    context.Advance(2);
                    builder.Append(next);
                    return;
                }
            }

            builder.Append(context.Current);
            context.Advance();
        }

        private void ReadSingleQuotedContent(TokenizeContext context, StringBuilder builder)
        {
            int quoteStart = context.Position - 1;

            while (context.HasMore)
            {
                char c = context.Current;

                if (c == '\'')
                {
                    context.Advance();
                    return;
                }

                builder.Append(c);
                context.Advance();
            }

            throw new ParseException($"Unclosed single quote starting at position {quoteStart}");
        }

        #endregion

        #region Context

        private class TokenizeContext
        {
            private readonly string input;
            private readonly int length;

            public int Position { get; private set; }
            public bool HasMore => Position < length;
            public bool HasNext => Position + 1 < length;
            public char Current => input[Position];

            public TokenizeContext(string input)
            {
                this.input = input;
                this.length = input.Length;
                Position = 0;
            }

            public char PeekNext() => input[Position + 1];

            public void Advance(int count = 1) => Position += count;
        }

        #endregion
    }
}
