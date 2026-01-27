using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// tailコマンドのテスト。
    /// </summary>
    public class TailCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalTailTests");
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

        // TAIL-001 デフォルト（10行）
        [Test]
        public async Task Tail_Default_OutputsLast10Lines()
        {
            var lines = new string[15];
            for (int i = 0; i < 15; i++)
                lines[i] = $"line{i + 1}";

            CreateFile("test.txt", lines);

            var exitCode = await terminal.ExecuteAsync("tail test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("line5"));
            Assert.IsTrue(output.Contains("line6"));
            Assert.IsTrue(output.Contains("line15"));
        }

        // TAIL-002 行数指定
        [Test]
        public async Task Tail_LinesOption_OutputsSpecifiedLines()
        {
            CreateFile("test.txt", "line1", "line2", "line3", "line4", "line5");

            var exitCode = await terminal.ExecuteAsync("tail -n=3 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("line1"));
            Assert.IsFalse(output.Contains("line2"));
            Assert.IsTrue(output.Contains("line3"));
            Assert.IsTrue(output.Contains("line5"));
        }

        // TAIL-003 先頭から（+K形式）
        [Test]
        public async Task Tail_FromStart_OutputsFromLineK()
        {
            CreateFile("test.txt", "line1", "line2", "line3", "line4", "line5");

            var exitCode = await terminal.ExecuteAsync("tail -n=+3 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("line1"));
            Assert.IsFalse(output.Contains("line2"));
            Assert.IsTrue(output.Contains("line3"));
            Assert.IsTrue(output.Contains("line4"));
            Assert.IsTrue(output.Contains("line5"));
        }

        // TAIL-004 バイト数指定
        [Test]
        public async Task Tail_BytesOption_OutputsSpecifiedBytes()
        {
            CreateFileWithContent("test.txt", "Hello, World!");

            var exitCode = await terminal.ExecuteAsync("tail -c=6 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual("World!", stdout.ToString());
        }

        // TAIL-005 10行未満のファイル
        [Test]
        public async Task Tail_LessThan10Lines_OutputsAllLines()
        {
            CreateFile("test.txt", "line1", "line2", "line3");

            var exitCode = await terminal.ExecuteAsync("tail test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("line1"));
            Assert.IsTrue(output.Contains("line2"));
            Assert.IsTrue(output.Contains("line3"));
        }

        #endregion

        #region 複数ファイルテスト

        // TAIL-010 複数ファイル（ヘッダー表示）
        [Test]
        public async Task Tail_MultipleFiles_ShowsHeaders()
        {
            CreateFile("file1.txt", "content1");
            CreateFile("file2.txt", "content2");

            var exitCode = await terminal.ExecuteAsync("tail file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("==> file1.txt <=="));
            Assert.IsTrue(output.Contains("==> file2.txt <=="));
            Assert.IsTrue(output.Contains("content1"));
            Assert.IsTrue(output.Contains("content2"));
        }

        // TAIL-011 単一ファイル（ヘッダー非表示）
        [Test]
        public async Task Tail_SingleFile_NoHeader()
        {
            CreateFile("test.txt", "content");

            var exitCode = await terminal.ExecuteAsync("tail test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("==>"));
        }

        // TAIL-012 -v オプション（単一ファイルでもヘッダー表示）
        [Test]
        public async Task Tail_VerboseOption_ShowsHeader()
        {
            CreateFile("test.txt", "content");

            var exitCode = await terminal.ExecuteAsync("tail -v test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("==> test.txt <=="));
        }

        // TAIL-013 -q オプション（複数ファイルでもヘッダー非表示）
        [Test]
        public async Task Tail_QuietOption_NoHeaders()
        {
            CreateFile("file1.txt", "content1");
            CreateFile("file2.txt", "content2");

            var exitCode = await terminal.ExecuteAsync("tail -q file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("==>"));
        }

        #endregion

        #region 標準入力テスト

        // TAIL-020 パイプからの入力
        [Test]
        public async Task Tail_PipeInput_ProcessesStdin()
        {
            var exitCode = await terminal.ExecuteAsync("echo line1 | tail -n=1", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("line1"));
        }

        #endregion

        #region エラーケーステスト

        // TAIL-030 存在しないファイル
        [Test]
        public async Task Tail_NonExistentFile_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("tail nonexistent.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file"));
        }

        // TAIL-031 -n と -c の両方指定
        [Test]
        public async Task Tail_BothLinesAndBytes_ReturnsError()
        {
            CreateFile("test.txt", "content");

            var exitCode = await terminal.ExecuteAsync("tail -n=5 -c=10 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("cannot specify both"));
        }

        // TAIL-032 -f オプションでファイル指定なし
        [Test]
        public async Task Tail_FollowWithoutFile_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("tail -f", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("requires a file"));
        }

        // TAIL-033 -f オプションで複数ファイル
        [Test]
        public async Task Tail_FollowWithMultipleFiles_ReturnsError()
        {
            CreateFile("file1.txt", "content1");
            CreateFile("file2.txt", "content2");

            var exitCode = await terminal.ExecuteAsync("tail -f file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("multiple files is not supported"));
        }

        // TAIL-034 空ファイル
        [Test]
        public async Task Tail_EmptyFile_Success()
        {
            CreateFile("empty.txt");

            var exitCode = await terminal.ExecuteAsync("tail empty.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsEmpty(stdout.ToString().Trim());
        }

        #endregion
    }
}
