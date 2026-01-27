using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// headコマンドのテスト。
    /// </summary>
    public class HeadCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalHeadTests");
            Directory.CreateDirectory(testDir);

            terminal = new Terminal(testDir, testDir, registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
        }

        [TearDown]
        public void TearDown()
        {
            terminal?.Dispose();
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }

        private void CreateFile(string name, params string[] lines)
        {
            File.WriteAllLines(Path.Combine(testDir, name), lines);
        }

        private void CreateFileWithContent(string name, string content)
        {
            File.WriteAllText(Path.Combine(testDir, name), content);
        }

        #region 基本動作テスト

        // HEAD-001 デフォルト（10行）
        [Test]
        public async Task Head_Default_OutputsFirst10Lines()
        {
            var lines = new string[15];
            for (int i = 0; i < 15; i++)
                lines[i] = $"line{i + 1}";

            CreateFile("test.txt", lines);

            var exitCode = await terminal.ExecuteAsync("head test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("line1"));
            Assert.IsTrue(output.Contains("line10"));
            Assert.IsFalse(output.Contains("line11"));
        }

        // HEAD-002 行数指定
        [Test]
        public async Task Head_LinesOption_OutputsSpecifiedLines()
        {
            CreateFile("test.txt", "line1", "line2", "line3", "line4", "line5");

            var exitCode = await terminal.ExecuteAsync("head -n 3 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("line1"));
            Assert.IsTrue(output.Contains("line3"));
            Assert.IsFalse(output.Contains("line4"));
        }

        // HEAD-003 末尾除外（-K形式）
        [Test]
        public async Task Head_ExcludeLastLines_OutputsAllExceptLastK()
        {
            CreateFile("test.txt", "line1", "line2", "line3", "line4", "line5");

            var exitCode = await terminal.ExecuteAsync("head -n=-2 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("line1"));
            Assert.IsTrue(output.Contains("line3"));
            Assert.IsFalse(output.Contains("line4"));
            Assert.IsFalse(output.Contains("line5"));
        }

        // HEAD-004 バイト数指定
        [Test]
        public async Task Head_BytesOption_OutputsSpecifiedBytes()
        {
            CreateFileWithContent("test.txt", "Hello, World!");

            var exitCode = await terminal.ExecuteAsync("head -c 5 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual("Hello", stdout.ToString());
        }

        // HEAD-005 10行未満のファイル
        [Test]
        public async Task Head_LessThan10Lines_OutputsAllLines()
        {
            CreateFile("test.txt", "line1", "line2", "line3");

            var exitCode = await terminal.ExecuteAsync("head test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("line1"));
            Assert.IsTrue(output.Contains("line2"));
            Assert.IsTrue(output.Contains("line3"));
        }

        #endregion

        #region 複数ファイルテスト

        // HEAD-010 複数ファイル（ヘッダー表示）
        [Test]
        public async Task Head_MultipleFiles_ShowsHeaders()
        {
            CreateFile("file1.txt", "content1");
            CreateFile("file2.txt", "content2");

            var exitCode = await terminal.ExecuteAsync("head file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("==> file1.txt <=="));
            Assert.IsTrue(output.Contains("==> file2.txt <=="));
            Assert.IsTrue(output.Contains("content1"));
            Assert.IsTrue(output.Contains("content2"));
        }

        // HEAD-011 単一ファイル（ヘッダー非表示）
        [Test]
        public async Task Head_SingleFile_NoHeader()
        {
            CreateFile("test.txt", "content");

            var exitCode = await terminal.ExecuteAsync("head test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("==>"));
        }

        // HEAD-012 -v オプション（単一ファイルでもヘッダー表示）
        [Test]
        public async Task Head_VerboseOption_ShowsHeader()
        {
            CreateFile("test.txt", "content");

            var exitCode = await terminal.ExecuteAsync("head -v test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("==> test.txt <=="));
        }

        // HEAD-013 -q オプション（複数ファイルでもヘッダー非表示）
        [Test]
        public async Task Head_QuietOption_NoHeaders()
        {
            CreateFile("file1.txt", "content1");
            CreateFile("file2.txt", "content2");

            var exitCode = await terminal.ExecuteAsync("head -q file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("==>"));
        }

        #endregion

        #region 標準入力テスト

        // HEAD-020 パイプからの入力
        [Test]
        public async Task Head_PipeInput_ProcessesStdin()
        {
            var exitCode = await terminal.ExecuteAsync("echo line1 | head -n 1", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("line1"));
        }

        #endregion

        #region エラーケーステスト

        // HEAD-030 存在しないファイル
        [Test]
        public async Task Head_NonExistentFile_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("head nonexistent.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file"));
        }

        // HEAD-031 -n と -c の両方指定
        [Test]
        public async Task Head_BothLinesAndBytes_ReturnsError()
        {
            CreateFile("test.txt", "content");

            var exitCode = await terminal.ExecuteAsync("head -n 5 -c 10 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("cannot specify both"));
        }

        // HEAD-032 空ファイル
        [Test]
        public async Task Head_EmptyFile_Success()
        {
            CreateFile("empty.txt");

            var exitCode = await terminal.ExecuteAsync("head empty.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsEmpty(stdout.ToString().Trim());
        }

        #endregion
    }
}
