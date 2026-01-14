using NUnit.Framework;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Tests
{
    public class ParserTests
    {
        private Parser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new Parser();
        }

        // PRS-001 単一コマンド
        [Test]
        public void Parse_SingleCommand_ReturnsCorrectStructure()
        {
            var result = parser.Parse("echo a b");

            Assert.IsFalse(result.IsEmpty);
            Assert.AreEqual(1, result.Pipeline.Commands.Count);
            Assert.AreEqual("echo", result.Pipeline.Commands[0].CommandName);
            Assert.AreEqual(2, result.Pipeline.Commands[0].PositionalArguments.Count);
            Assert.AreEqual("a", result.Pipeline.Commands[0].PositionalArguments[0]);
            Assert.AreEqual("b", result.Pipeline.Commands[0].PositionalArguments[1]);
        }

        // PRS-010 --は以降を位置引数として扱う
        [Test]
        public void Parse_EndOfOptions_TreatsFollowingAsPositional()
        {
            var result = parser.Parse("cmd -- --x -y");

            Assert.AreEqual(1, result.Pipeline.Commands.Count);
            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual("cmd", cmd.CommandName);
            Assert.AreEqual(0, cmd.Options.Count);
            Assert.AreEqual(2, cmd.PositionalArguments.Count);
            Assert.AreEqual("--x", cmd.PositionalArguments[0]);
            Assert.AreEqual("-y", cmd.PositionalArguments[1]);
        }

        // PRS-020 2段階パイプ
        [Test]
        public void Parse_TwoStagePipe_ReturnsTwoCommands()
        {
            var result = parser.Parse("a | b");

            Assert.AreEqual(2, result.Pipeline.Commands.Count);
            Assert.AreEqual("a", result.Pipeline.Commands[0].CommandName);
            Assert.AreEqual("b", result.Pipeline.Commands[1].CommandName);
        }

        // PRS-030 標準入力リダイレクト
        [Test]
        public void Parse_StdinRedirect_SetsStdinPath()
        {
            var result = parser.Parse("cat < in.txt");

            Assert.AreEqual(1, result.Pipeline.Commands.Count);
            Assert.AreEqual("in.txt", result.Pipeline.Commands[0].Redirections.StdinPath);
        }

        // PRS-031 標準出力上書き
        [Test]
        public void Parse_StdoutOverwrite_SetsStdoutPath()
        {
            var result = parser.Parse("echo a > out.txt");

            Assert.AreEqual(1, result.Pipeline.Commands.Count);
            Assert.AreEqual("out.txt", result.Pipeline.Commands[0].Redirections.StdoutPath);
            Assert.AreEqual(RedirectMode.Overwrite, result.Pipeline.Commands[0].Redirections.StdoutMode);
        }

        // PRS-032 標準出力追記
        [Test]
        public void Parse_StdoutAppend_SetsAppendMode()
        {
            var result = parser.Parse("echo a >> out.txt");

            Assert.AreEqual(1, result.Pipeline.Commands.Count);
            Assert.AreEqual("out.txt", result.Pipeline.Commands[0].Redirections.StdoutPath);
            Assert.AreEqual(RedirectMode.Append, result.Pipeline.Commands[0].Redirections.StdoutMode);
        }

        // PRS-040 リダイレクト出力の後にパイプはエラー
        [Test]
        public void Parse_RedirectOutFollowedByPipe_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => parser.Parse("echo a > out.txt | next"));
        }

        // PRS-041 ファイル名なしのリダイレクト
        [Test]
        public void Parse_RedirectOutWithoutFilename_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => parser.Parse("echo a >"));
        }

        // PRS-043 末尾パイプ
        [Test]
        public void Parse_TrailingPipe_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => parser.Parse("a |"));
        }

        // 追加テスト
        [Test]
        public void Parse_EmptyInput_ReturnsEmpty()
        {
            var result = parser.Parse("");
            Assert.IsTrue(result.IsEmpty);
        }

        [Test]
        public void Parse_LongOption_ParsedCorrectly()
        {
            var result = parser.Parse("cmd --verbose --name=value");

            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual(2, cmd.Options.Count);
            Assert.AreEqual("verbose", cmd.Options[0].Name);
            Assert.IsTrue(cmd.Options[0].IsLong);
            Assert.IsFalse(cmd.Options[0].HasValue);
            Assert.AreEqual("name", cmd.Options[1].Name);
            Assert.IsTrue(cmd.Options[1].IsLong);
            Assert.IsTrue(cmd.Options[1].HasValue);
            Assert.AreEqual("value", cmd.Options[1].RawValue);
        }

        [Test]
        public void Parse_ShortOption_ParsedCorrectly()
        {
            var result = parser.Parse("cmd -v -n=value");

            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual(2, cmd.Options.Count);
            Assert.AreEqual("v", cmd.Options[0].Name);
            Assert.IsFalse(cmd.Options[0].IsLong);
            Assert.AreEqual("n", cmd.Options[1].Name);
            Assert.AreEqual("value", cmd.Options[1].RawValue);
        }

        [Test]
        public void Parse_BundledShortOptions_ParsedAsSeparate()
        {
            var result = parser.Parse("cmd -abc");

            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual(3, cmd.Options.Count);
            Assert.AreEqual("a", cmd.Options[0].Name);
            Assert.AreEqual("b", cmd.Options[1].Name);
            Assert.AreEqual("c", cmd.Options[2].Name);
        }

        [Test]
        public void Parse_MultiplePipes_ParsedCorrectly()
        {
            var result = parser.Parse("a | b | c");

            Assert.AreEqual(3, result.Pipeline.Commands.Count);
            Assert.AreEqual("a", result.Pipeline.Commands[0].CommandName);
            Assert.AreEqual("b", result.Pipeline.Commands[1].CommandName);
            Assert.AreEqual("c", result.Pipeline.Commands[2].CommandName);
        }

        [Test]
        public void Parse_StdinRedirectWithPipe_Allowed()
        {
            var result = parser.Parse("cat < in.txt | grep foo");

            Assert.AreEqual(2, result.Pipeline.Commands.Count);
            Assert.AreEqual("in.txt", result.Pipeline.Commands[0].Redirections.StdinPath);
            Assert.AreEqual("grep", result.Pipeline.Commands[1].CommandName);
        }

        [Test]
        public void Parse_NegativeNumber_TreatedAsPositional()
        {
            var result = parser.Parse("cmd -5");

            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual(0, cmd.Options.Count);
            Assert.AreEqual(1, cmd.PositionalArguments.Count);
            Assert.AreEqual("-5", cmd.PositionalArguments[0]);
        }

        [Test]
        public void Parse_QuotedOptionValue_ParsedCorrectly()
        {
            var result = parser.Parse("cmd --name=\"hello world\"");

            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual(1, cmd.Options.Count);
            Assert.AreEqual("name", cmd.Options[0].Name);
            Assert.AreEqual("hello world", cmd.Options[0].RawValue);
        }

        [Test]
        public void Parse_LeadingPipe_ThrowsParseException()
        {
            Assert.Throws<ParseException>(() => parser.Parse("| cmd"));
        }

        [Test]
        public void Parse_EmptyOptionValue_ParsedAsEmptyString()
        {
            var result = parser.Parse("cmd --name=");

            var cmd = result.Pipeline.Commands[0];
            Assert.AreEqual(1, cmd.Options.Count);
            Assert.AreEqual("name", cmd.Options[0].Name);
            Assert.IsTrue(cmd.Options[0].HasValue);
            Assert.AreEqual("", cmd.Options[0].RawValue);
        }
    }
}
