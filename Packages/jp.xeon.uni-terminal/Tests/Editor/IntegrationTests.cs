using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    public class IntegrationTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalTests");
            Directory.CreateDirectory(testDir);

            terminal = new Terminal(testDir, testDir, registerBuiltInCommands: true);

            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }

        // EXE-001 行単位パイプ
        [Test]
        public async Task Execute_LinePipe_Works()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo foo | grep --pattern=foo",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("foo"));
        }

        // EXE-020 不明なオプションでパイプライン停止
        [Test]
        public async Task Execute_UnknownOption_StopsPipeline()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo foo | grep --unknown=1 | echo bar",
                stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("unknown option"));
        }

        // IO-001 標準入力リダイレクト
        [Test]
        public async Task Execute_StdinRedirect_Works()
        {
            var testFile = Path.Combine(testDir, "in.txt");
            File.WriteAllText(testFile, "foo\nbar\n");

            var exitCode = await terminal.ExecuteAsync(
                "cat < in.txt",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("foo"));
            Assert.IsTrue(stdout.ToString().Contains("bar"));
        }

        // IO-010 標準出力上書き
        [Test]
        public async Task Execute_StdoutOverwrite_Works()
        {
            var outFile = Path.Combine(testDir, "out.txt");

            var exitCode = await terminal.ExecuteAsync(
                "echo hello > out.txt",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(File.Exists(outFile));
            Assert.IsTrue(File.ReadAllText(outFile).Contains("hello"));
        }

        // IO-011 標準出力追記
        [Test]
        public async Task Execute_StdoutAppend_Works()
        {
            var outFile = Path.Combine(testDir, "out.txt");
            File.WriteAllText(outFile, "first\n");

            var exitCode = await terminal.ExecuteAsync(
                "echo second >> out.txt",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var content = File.ReadAllText(outFile);
            Assert.IsTrue(content.Contains("first"));
            Assert.IsTrue(content.Contains("second"));
        }

        // 追加テスト
        [Test]
        public async Task Execute_Echo_OutputsArguments()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo hello world",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("hello world"));
        }

        [Test]
        public async Task Execute_GrepWithPattern_FiltersLines()
        {
            var testFile = Path.Combine(testDir, "test.txt");
            File.WriteAllText(testFile, "apple\nbanana\napricot\n");

            var exitCode = await terminal.ExecuteAsync(
                "cat test.txt | grep --pattern=^a",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("apple"));
            Assert.IsTrue(output.Contains("apricot"));
            Assert.IsFalse(output.Contains("banana"));
        }

        [Test]
        public async Task Execute_Help_ShowsCommands()
        {
            var exitCode = await terminal.ExecuteAsync(
                "help",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("echo"));
            Assert.IsTrue(output.Contains("grep"));
        }

        [Test]
        public async Task Execute_CommandNotFound_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync(
                "nonexistent",
                stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("command not found"));
        }

        [Test]
        public async Task Execute_ParseError_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo \"unclosed",
                stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("Parse error"));
        }

        [Test]
        public async Task Execute_MultiplePipes_Works()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo one two three | grep --pattern=two | grep --pattern=two",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("two"));
        }

        [Test]
        public async Task Execute_EmptyInput_ReturnsSuccess()
        {
            var exitCode = await terminal.ExecuteAsync(
                "",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        [Test]
        public async Task Execute_QuotedArguments_HandledCorrectly()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo \"hello world\" 'single quoted'",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("hello world"));
            Assert.IsTrue(output.Contains("single quoted"));
        }

        [Test]
        public async Task Execute_EndOfOptions_WorksCorrectly()
        {
            var exitCode = await terminal.ExecuteAsync(
                "echo -- --not-an-option",
                stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("--not-an-option"));
        }
    }
}
