using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    public class IntegrationTests
    {
        private Terminal _terminal;
        private StringBuilderTextWriter _stdout;
        private StringBuilderTextWriter _stderr;
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "UniTerminalTests");
            Directory.CreateDirectory(_testDir);

            _terminal = new Terminal(_testDir, _testDir, registerBuiltInCommands: true);

            _stdout = new StringBuilderTextWriter();
            _stderr = new StringBuilderTextWriter();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        // EXE-001 行単位パイプ
        [Test]
        public async Task Execute_LinePipe_Works()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo foo | grep --pattern=foo",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("foo"));
        }

        // EXE-020 不明なオプションでパイプライン停止
        [Test]
        public async Task Execute_UnknownOption_StopsPipeline()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo foo | grep --unknown=1 | echo bar",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("unknown option"));
        }

        // IO-001 標準入力リダイレクト
        [Test]
        public async Task Execute_StdinRedirect_Works()
        {
            var testFile = Path.Combine(_testDir, "in.txt");
            File.WriteAllText(testFile, "foo\nbar\n");

            var exitCode = await _terminal.ExecuteAsync(
                "cat < in.txt",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("foo"));
            Assert.IsTrue(_stdout.ToString().Contains("bar"));
        }

        // IO-010 標準出力上書き
        [Test]
        public async Task Execute_StdoutOverwrite_Works()
        {
            var outFile = Path.Combine(_testDir, "out.txt");

            var exitCode = await _terminal.ExecuteAsync(
                "echo hello > out.txt",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(File.Exists(outFile));
            Assert.IsTrue(File.ReadAllText(outFile).Contains("hello"));
        }

        // IO-011 標準出力追記
        [Test]
        public async Task Execute_StdoutAppend_Works()
        {
            var outFile = Path.Combine(_testDir, "out.txt");
            File.WriteAllText(outFile, "first\n");

            var exitCode = await _terminal.ExecuteAsync(
                "echo second >> out.txt",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var content = File.ReadAllText(outFile);
            Assert.IsTrue(content.Contains("first"));
            Assert.IsTrue(content.Contains("second"));
        }

        // 追加テスト
        [Test]
        public async Task Execute_Echo_OutputsArguments()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo hello world",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("hello world"));
        }

        [Test]
        public async Task Execute_GrepWithPattern_FiltersLines()
        {
            var testFile = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(testFile, "apple\nbanana\napricot\n");

            var exitCode = await _terminal.ExecuteAsync(
                "cat test.txt | grep --pattern=^a",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains("apple"));
            Assert.IsTrue(output.Contains("apricot"));
            Assert.IsFalse(output.Contains("banana"));
        }

        [Test]
        public async Task Execute_Help_ShowsCommands()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "help",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains("echo"));
            Assert.IsTrue(output.Contains("grep"));
        }

        [Test]
        public async Task Execute_CommandNotFound_ReturnsError()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "nonexistent",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("command not found"));
        }

        [Test]
        public async Task Execute_ParseError_ReturnsError()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo \"unclosed",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("Parse error"));
        }

        [Test]
        public async Task Execute_MultiplePipes_Works()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo one two three | grep --pattern=two | grep --pattern=two",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("two"));
        }

        [Test]
        public async Task Execute_EmptyInput_ReturnsSuccess()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        [Test]
        public async Task Execute_QuotedArguments_HandledCorrectly()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo \"hello world\" 'single quoted'",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains("hello world"));
            Assert.IsTrue(output.Contains("single quoted"));
        }

        [Test]
        public async Task Execute_EndOfOptions_WorksCorrectly()
        {
            var exitCode = await _terminal.ExecuteAsync(
                "echo -- --not-an-option",
                _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("--not-an-option"));
        }
    }
}
