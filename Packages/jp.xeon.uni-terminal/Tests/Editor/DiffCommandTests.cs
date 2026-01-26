using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// diffコマンドのテスト
    /// </summary>
    public class DiffCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalDiffTests");
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

        private void CreateFile(string name, params string[] lines)
        {
            File.WriteAllLines(Path.Combine(testDir, name), lines);
        }

        // DIFF-001 同一ファイル
        [Test]
        public async Task Diff_SameFiles_NoDifference()
        {
            CreateFile("file1.txt", "line1", "line2", "line3");
            CreateFile("file2.txt", "line1", "line2", "line3");

            var exitCode = await terminal.ExecuteAsync("diff file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsEmpty(stdout.ToString().Trim());
        }

        // DIFF-002 異なるファイル
        [Test]
        public async Task Diff_DifferentFiles_ShowsDifference()
        {
            CreateFile("file1.txt", "line1", "line2", "line3");
            CreateFile("file2.txt", "line1", "modified", "line3");

            var exitCode = await terminal.ExecuteAsync("diff file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual((ExitCode)1, exitCode); // 差分あり
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("line2") || output.Contains("modified"));
        }

        // DIFF-003 行追加検出
        [Test]
        public async Task Diff_AddedLines()
        {
            CreateFile("file1.txt", "line1", "line2");
            CreateFile("file2.txt", "line1", "line2", "line3");

            var exitCode = await terminal.ExecuteAsync("diff file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual((ExitCode)1, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains(">") || output.Contains("+"));
        }

        // DIFF-004 行削除検出
        [Test]
        public async Task Diff_DeletedLines()
        {
            CreateFile("file1.txt", "line1", "line2", "line3");
            CreateFile("file2.txt", "line1", "line3");

            var exitCode = await terminal.ExecuteAsync("diff file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual((ExitCode)1, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("<") || output.Contains("-"));
        }

        // DIFF-010 Unified形式
        [Test]
        public async Task Diff_UnifiedFormat()
        {
            CreateFile("file1.txt", "line1", "line2", "line3");
            CreateFile("file2.txt", "line1", "modified", "line3");

            var exitCode = await terminal.ExecuteAsync("diff -u 3 file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual((ExitCode)1, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("---"));
            Assert.IsTrue(output.Contains("+++"));
            Assert.IsTrue(output.Contains("@@"));
        }

        // DIFF-012 Brief形式
        [Test]
        public async Task Diff_BriefFormat()
        {
            CreateFile("file1.txt", "line1", "line2");
            CreateFile("file2.txt", "line1", "different");

            var exitCode = await terminal.ExecuteAsync("diff -q file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual((ExitCode)1, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("differ"));
        }

        // DIFF-020 大文字小文字無視
        [Test]
        public async Task Diff_IgnoreCase()
        {
            CreateFile("file1.txt", "LINE1", "LINE2");
            CreateFile("file2.txt", "line1", "line2");

            var exitCode = await terminal.ExecuteAsync("diff -i file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        // DIFF-021 空白変更無視
        [Test]
        public async Task Diff_IgnoreSpaceChange()
        {
            CreateFile("file1.txt", "line 1", "line  2");
            CreateFile("file2.txt", "line  1", "line 2");

            var exitCode = await terminal.ExecuteAsync("diff -b file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        // DIFF-022 全空白無視
        [Test]
        public async Task Diff_IgnoreAllSpace()
        {
            CreateFile("file1.txt", "line1", "line2");
            CreateFile("file2.txt", "line 1", "line 2");

            var exitCode = await terminal.ExecuteAsync("diff -w file1.txt file2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        // DIFF-030 存在しないファイル
        [Test]
        public async Task Diff_NonExistentFile()
        {
            CreateFile("file1.txt", "line1");

            var exitCode = await terminal.ExecuteAsync("diff file1.txt nonexistent.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file"));
        }

        // DIFF-031 引数不足
        [Test]
        public async Task Diff_MissingOperand()
        {
            CreateFile("file1.txt", "line1");

            var exitCode = await terminal.ExecuteAsync("diff file1.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("missing operand"));
        }

        // DIFF-032 ディレクトリ指定
        [Test]
        public async Task Diff_Directory()
        {
            CreateFile("file1.txt", "line1");
            Directory.CreateDirectory(Path.Combine(testDir, "subdir"));

            var exitCode = await terminal.ExecuteAsync("diff file1.txt subdir", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("Is a directory"));
        }

        // 空ファイル同士
        [Test]
        public async Task Diff_EmptyFiles()
        {
            CreateFile("empty1.txt");
            CreateFile("empty2.txt");

            var exitCode = await terminal.ExecuteAsync("diff empty1.txt empty2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        // Brief形式で同一ファイル
        [Test]
        public async Task Diff_Brief_SameFiles()
        {
            CreateFile("same1.txt", "line1", "line2");
            CreateFile("same2.txt", "line1", "line2");

            var exitCode = await terminal.ExecuteAsync("diff -q same1.txt same2.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsEmpty(stdout.ToString().Trim());
        }
    }
}
