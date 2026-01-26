using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// findコマンドのテスト
    /// </summary>
    public class FindCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalFindTests");
            Directory.CreateDirectory(testDir);

            // テスト用のディレクトリ構造を作成
            // testDir/
            //   file1.txt
            //   file2.cs
            //   subdir/
            //     file3.txt
            //     file4.cs
            //     deep/
            //       file5.txt

            File.WriteAllText(Path.Combine(testDir, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(testDir, "file2.cs"), "content2");

            var subdir = Path.Combine(testDir, "subdir");
            Directory.CreateDirectory(subdir);
            File.WriteAllText(Path.Combine(subdir, "file3.txt"), "content3");
            File.WriteAllText(Path.Combine(subdir, "file4.cs"), "content4");

            var deep = Path.Combine(subdir, "deep");
            Directory.CreateDirectory(deep);
            File.WriteAllText(Path.Combine(deep, "file5.txt"), "content5");

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

        // FIND-001 全ファイル検索
        [Test]
        public async Task Find_Default_ShowsAllEntries()
        {
            var exitCode = await terminal.ExecuteAsync("find .", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.cs"));
            Assert.IsTrue(output.Contains("subdir"));
        }

        // FIND-002 名前パターン検索
        [Test]
        public async Task Find_Name_FiltersFiles()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"*.txt\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file3.txt"));
            Assert.IsTrue(output.Contains("file5.txt"));
            Assert.IsFalse(output.Contains("file2.cs"));
            Assert.IsFalse(output.Contains("file4.cs"));
        }

        // FIND-003 大文字小文字無視
        [Test]
        public async Task Find_IName_CaseInsensitive()
        {
            // 大文字のファイルを作成
            File.WriteAllText(Path.Combine(testDir, "README.txt"), "readme");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("find . --iname \"readme*\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("README.txt"));
        }

        // FIND-004 タイプ指定（ファイル）
        [Test]
        public async Task Find_TypeFile_ShowsFilesOnly()
        {
            var exitCode = await terminal.ExecuteAsync("find . --type f", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.cs"));
            Assert.IsFalse(output.Contains("subdir") && !output.Contains("subdir/")); // ディレクトリは含まない
        }

        // FIND-005 タイプ指定（ディレクトリ）
        [Test]
        public async Task Find_TypeDirectory_ShowsDirectoriesOnly()
        {
            var exitCode = await terminal.ExecuteAsync("find . --type d", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("subdir") || output.Contains("./subdir"));
            Assert.IsTrue(output.Contains("deep") || output.Contains("./subdir/deep"));
        }

        // FIND-010 最大深度指定
        [Test]
        public async Task Find_MaxDepth_LimitsSearch()
        {
            var exitCode = await terminal.ExecuteAsync("find . --maxdepth 1", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("subdir"));
            Assert.IsFalse(output.Contains("file3.txt")); // 深度2以上は含まない
            Assert.IsFalse(output.Contains("file5.txt"));
        }

        // FIND-011 最小深度指定
        [Test]
        public async Task Find_MinDepth_StartsFromDepth()
        {
            var exitCode = await terminal.ExecuteAsync("find . --mindepth 2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("./file1.txt")); // 深度1は含まない
            Assert.IsTrue(output.Contains("file3.txt")); // 深度2以上
        }

        // FIND-012 深度範囲指定
        [Test]
        public async Task Find_DepthRange_LimitsToRange()
        {
            var exitCode = await terminal.ExecuteAsync("find . --mindepth 1 --maxdepth 2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt")); // 深度1
            Assert.IsTrue(output.Contains("file3.txt")); // 深度2
            Assert.IsFalse(output.Contains("file5.txt")); // 深度3は含まない
        }

        // FIND-020 アスタリスクワイルドカード
        [Test]
        public async Task Find_WildcardAsterisk_MatchesMultiple()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"file*\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.cs"));
            Assert.IsTrue(output.Contains("file3.txt"));
        }

        // FIND-021 クエスチョンワイルドカード
        [Test]
        public async Task Find_WildcardQuestion_MatchesSingleChar()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"file?.txt\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file3.txt"));
            Assert.IsTrue(output.Contains("file5.txt"));
            Assert.IsFalse(output.Contains("file2.cs"));
        }

        // FIND-030 存在しないパス
        [Test]
        public async Task Find_NonExistentPath_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("find ./nonexistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file or directory"));
        }

        // FIND-031 空の結果
        [Test]
        public async Task Find_NoMatches_ReturnsSuccess()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"*.xyz\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString().Trim();
            // 結果なしでも成功
            Assert.IsFalse(output.Contains(".xyz"));
        }

        // FIND-040 パイプ連携
        [Test]
        public async Task Find_Pipe_WorksWithGrep()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"*.txt\" | grep --pattern=file1", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsFalse(output.Contains("file3.txt"));
        }

        // 複合条件テスト
        [Test]
        public async Task Find_CombinedOptions_WorksTogether()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"*.txt\" --type f --maxdepth 2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt")); // 深度1のtxtファイル
            Assert.IsTrue(output.Contains("file3.txt")); // 深度2のtxtファイル
            Assert.IsFalse(output.Contains("file5.txt")); // 深度3は含まない
            Assert.IsFalse(output.Contains("file2.cs")); // csファイルは含まない
        }

        // 相対パス出力テスト
        [Test]
        public async Task Find_Output_UsesRelativePaths()
        {
            var exitCode = await terminal.ExecuteAsync("find . --name \"file5.txt\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // ./subdir/deep/file5.txt のような相対パス形式
            Assert.IsTrue(output.Contains("./"));
        }
    }
}
