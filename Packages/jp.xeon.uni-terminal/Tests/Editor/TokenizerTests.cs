using NUnit.Framework;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Tests
{
    public class TokenizerTests
    {
        private Tokenizer _tokenizer;

        [SetUp]
        public void SetUp()
        {
            _tokenizer = new Tokenizer();
        }

        // TKN-001 単一スペース区切り
        [Test]
        public void Tokenize_SingleSpaceDelimiter_ReturnsCorrectTokens()
        {
            var tokens = _tokenizer.Tokenize("echo a b");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a", tokens[1].Value);
            Assert.AreEqual("b", tokens[2].Value);
        }

        // TKN-002 連続スペース
        [Test]
        public void Tokenize_ConsecutiveSpaces_ReturnsCorrectTokens()
        {
            var tokens = _tokenizer.Tokenize("echo  a   b");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a", tokens[1].Value);
            Assert.AreEqual("b", tokens[2].Value);
        }

        // TKN-003 先頭と末尾のスペース
        [Test]
        public void Tokenize_LeadingTrailingSpaces_ReturnsCorrectTokens()
        {
            var tokens = _tokenizer.Tokenize("  echo a  ");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a", tokens[1].Value);
        }

        // TKN-010 タブ文字エラー
        [Test]
        public void Tokenize_TabCharacter_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => _tokenizer.Tokenize("echo\ta"));
        }

        // TKN-020 ダブルクォート
        [Test]
        public void Tokenize_DoubleQuotes_ReturnsQuotedContent()
        {
            var tokens = _tokenizer.Tokenize("echo \"a b\"");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a b", tokens[1].Value);
            Assert.IsTrue(tokens[1].WasQuoted);
        }

        // TKN-021 シングルクォート
        [Test]
        public void Tokenize_SingleQuotes_ReturnsQuotedContent()
        {
            var tokens = _tokenizer.Tokenize("echo 'a b'");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a b", tokens[1].Value);
            Assert.IsTrue(tokens[1].WasQuoted);
        }

        // TKN-022 空文字列（ダブルクォート）
        [Test]
        public void Tokenize_EmptyDoubleQuotes_ReturnsEmptyString()
        {
            var tokens = _tokenizer.Tokenize("echo \"\"");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("", tokens[1].Value);
            Assert.IsTrue(tokens[1].WasQuoted);
        }

        // TKN-023 空文字列（シングルクォート）
        [Test]
        public void Tokenize_EmptySingleQuotes_ReturnsEmptyString()
        {
            var tokens = _tokenizer.Tokenize("echo ''");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("", tokens[1].Value);
            Assert.IsTrue(tokens[1].WasQuoted);
        }

        // TKN-030 クォート外のスペースエスケープ
        [Test]
        public void Tokenize_EscapeSpaceOutsideQuotes_ReturnsUnescaped()
        {
            var tokens = _tokenizer.Tokenize(@"echo a\ b");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a b", tokens[1].Value);
        }

        // TKN-031 クォート外のバックスラッシュエスケープ
        [Test]
        public void Tokenize_EscapeBackslashOutsideQuotes_ReturnsBackslash()
        {
            var tokens = _tokenizer.Tokenize(@"echo a\\b");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual(@"a\b", tokens[1].Value);
        }

        // TKN-032 ダブルクォート内のクォートエスケープ
        [Test]
        public void Tokenize_EscapeQuoteInDoubleQuotes_ReturnsQuote()
        {
            var tokens = _tokenizer.Tokenize("echo \"a\\\"b\"");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a\"b", tokens[1].Value);
        }

        // TKN-033 ダブルクォート内のバックスラッシュエスケープ
        [Test]
        public void Tokenize_EscapeBackslashInDoubleQuotes_ReturnsBackslash()
        {
            var tokens = _tokenizer.Tokenize("echo \"a\\\\b\"");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual(@"a\b", tokens[1].Value);
        }

        // TKN-034 シングルクォート内ではエスケープなし - 閉じられていないクォートになる
        [Test]
        public void Tokenize_EscapeInSingleQuotes_ThrowsParseException()
        {
            // 'a\'b' - \'はエスケープされないため、'a\'は完全な文字列になり
            // その後にb'が続くが、これは閉じられていないクォート
            Assert.Throws<ParseException>(() => _tokenizer.Tokenize("echo 'a\\'b'"));
        }

        // TKN-040 \nは変換されない
        [Test]
        public void Tokenize_BackslashN_NotConverted()
        {
            var tokens = _tokenizer.Tokenize(@"echo \n");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("n", tokens[1].Value); // \nはnをリテラルnにエスケープ
        }

        // TKN-050 閉じられていないダブルクォート
        [Test]
        public void Tokenize_UnclosedDoubleQuote_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => _tokenizer.Tokenize("echo \"a"));
        }

        // TKN-060 パイプ演算子
        [Test]
        public void Tokenize_PipeOperator_ReturnsSeparateTokens()
        {
            var tokens = _tokenizer.Tokenize("a|b");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("a", tokens[0].Value);
            Assert.AreEqual(TokenKind.Word, tokens[0].Kind);
            Assert.AreEqual("|", tokens[1].Value);
            Assert.AreEqual(TokenKind.Pipe, tokens[1].Kind);
            Assert.AreEqual("b", tokens[2].Value);
            Assert.AreEqual(TokenKind.Word, tokens[2].Kind);
        }

        // TKN-062 リダイレクト出力演算子
        [Test]
        public void Tokenize_RedirectOutOperator_ReturnsSeparateTokens()
        {
            var tokens = _tokenizer.Tokenize("echo a>out.txt");

            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a", tokens[1].Value);
            Assert.AreEqual(">", tokens[2].Value);
            Assert.AreEqual(TokenKind.RedirectOut, tokens[2].Kind);
            Assert.AreEqual("out.txt", tokens[3].Value);
        }

        // TKN-063 リダイレクト追記演算子
        [Test]
        public void Tokenize_RedirectAppendOperator_ReturnsSeparateTokens()
        {
            var tokens = _tokenizer.Tokenize("echo a>>out.txt");

            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a", tokens[1].Value);
            Assert.AreEqual(">>", tokens[2].Value);
            Assert.AreEqual(TokenKind.RedirectAppend, tokens[2].Kind);
            Assert.AreEqual("out.txt", tokens[3].Value);
        }

        // TKN-064 オプション終端マーカー
        [Test]
        public void Tokenize_EndOfOptionsMarker_ReturnsEndOfOptionsToken()
        {
            var tokens = _tokenizer.Tokenize("cmd -- --notOption");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("cmd", tokens[0].Value);
            Assert.AreEqual(TokenKind.Word, tokens[0].Kind);
            Assert.AreEqual("--", tokens[1].Value);
            Assert.AreEqual(TokenKind.EndOfOptions, tokens[1].Kind);
            Assert.AreEqual("--notOption", tokens[2].Value);
            Assert.AreEqual(TokenKind.Word, tokens[2].Kind);
        }

        // 追加テスト
        [Test]
        public void Tokenize_EmptyInput_ReturnsEmptyList()
        {
            var tokens = _tokenizer.Tokenize("");
            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void Tokenize_NullInput_ReturnsEmptyList()
        {
            var tokens = _tokenizer.Tokenize(null);
            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void Tokenize_OnlySpaces_ReturnsEmptyList()
        {
            var tokens = _tokenizer.Tokenize("   ");
            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void Tokenize_MixedQuotes_ParsesCorrectly()
        {
            var tokens = _tokenizer.Tokenize("echo \"a'b\" 'c\"d'");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("a'b", tokens[1].Value);
            Assert.AreEqual("c\"d", tokens[2].Value);
        }

        [Test]
        public void Tokenize_RedirectInOperator_ReturnsSeparateTokens()
        {
            var tokens = _tokenizer.Tokenize("cat<in.txt");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("cat", tokens[0].Value);
            Assert.AreEqual("<", tokens[1].Value);
            Assert.AreEqual(TokenKind.RedirectIn, tokens[1].Kind);
            Assert.AreEqual("in.txt", tokens[2].Value);
        }

        [Test]
        public void Tokenize_Unicode_PreservedAsIs()
        {
            var tokens = _tokenizer.Tokenize("echo こんにちは 世界");

            Assert.AreEqual(3, tokens.Count);
            Assert.AreEqual("echo", tokens[0].Value);
            Assert.AreEqual("こんにちは", tokens[1].Value);
            Assert.AreEqual("世界", tokens[2].Value);
        }

        [Test]
        public void Tokenize_SpanIsCorrect()
        {
            var tokens = _tokenizer.Tokenize("ab cd");

            Assert.AreEqual(2, tokens.Count);
            Assert.AreEqual(0, tokens[0].Span.Start);
            Assert.AreEqual(2, tokens[0].Span.Length);
            Assert.AreEqual(3, tokens[1].Span.Start);
            Assert.AreEqual(2, tokens[1].Span.Length);
        }
    }
}
