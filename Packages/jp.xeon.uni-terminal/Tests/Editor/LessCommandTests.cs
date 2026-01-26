using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// lessコマンドのテスト
    /// </summary>
    public class LessCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;
        private string testFile;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalLessTests");
            Directory.CreateDirectory(testDir);

            // テストファイルを作成（50行）
            testFile = Path.Combine(testDir, "test.txt");
            var lines = new string[50];
            for (int i = 0; i < 50; i++)
            {
                lines[i] = $"Line {i + 1}: This is test content for line number {i + 1}";
            }
            File.WriteAllLines(testFile, lines);

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

        // LESS-001 ファイル表示
        [Test]
        public async Task Less_ShowsFileContent()
        {
            var exitCode = await terminal.ExecuteAsync("less test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Line 1:"));
            Assert.IsTrue(output.Contains("Line 50:"));
        }

        // LESS-002 行数指定
        [Test]
        public async Task Less_LinesOption()
        {
            var exitCode = await terminal.ExecuteAsync("less --lines 10 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Line 1:"));
            Assert.IsTrue(output.Contains("Line 10:"));
            // 11行目以降は含まれない
            Assert.IsFalse(output.Contains("Line 11:"));
        }

        // LESS-003 開始行指定
        [Test]
        public async Task Less_FromLineOption()
        {
            var exitCode = await terminal.ExecuteAsync("less --from-line 25 test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("Line 1:"));
            Assert.IsTrue(output.Contains("Line 25:"));
            Assert.IsTrue(output.Contains("Line 50:"));
        }

        // LESS-004 行番号表示
        [Test]
        public async Task Less_LineNumbersOption()
        {
            var exitCode = await terminal.ExecuteAsync("less --line-numbers test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // 行番号がタブ区切りで表示される
            Assert.IsTrue(output.Contains("\t"));
        }

        // LESS-020 長い行の切り詰め
        [Test]
        public async Task Less_ChopLongLines()
        {
            // 長い行を持つファイルを作成
            var longFile = Path.Combine(testDir, "long.txt");
            File.WriteAllText(longFile, new string('A', 200));

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("less --chop-long-lines long.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // 行が切り詰められている
            Assert.IsTrue(output.Contains("..."));
        }

        // LESS-030 パイプ入力
        [Test]
        public async Task Less_PipeInput()
        {
            var exitCode = await terminal.ExecuteAsync("cat test.txt | less", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Line 1:"));
        }

        // LESS-040 存在しないファイル
        [Test]
        public async Task Less_NonExistentFile_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("less nonexistent.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file"));
        }

        // LESS-041 ディレクトリ指定
        [Test]
        public async Task Less_Directory_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("less .", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("Is a directory"));
        }

        // 空ファイル
        [Test]
        public async Task Less_EmptyFile_Success()
        {
            var emptyFile = Path.Combine(testDir, "empty.txt");
            File.WriteAllText(emptyFile, "");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("less empty.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        // 複数オプション組み合わせ
        [Test]
        public async Task Less_CombinedOptions()
        {
            var exitCode = await terminal.ExecuteAsync("less --from-line 10 --lines 5 --line-numbers test.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Line 10:"));
        }
    }
}
