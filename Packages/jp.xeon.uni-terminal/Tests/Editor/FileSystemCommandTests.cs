using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// ファイルシステム関連コマンド（pwd, cd, ls）のテスト。
    /// </summary>
    public class FileSystemCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;
        private string homeDir;

        [SetUp]
        public void SetUp()
        {
            // パスをスラッシュで正規化（Terminal内部でスラッシュに統一されるため）
            testDir = PathUtility.NormalizeToSlash(Path.Combine(Path.GetTempPath(), "UniTerminalFsTests"));
            homeDir = PathUtility.NormalizeToSlash(Path.Combine(testDir, "home"));

            // テストディレクトリ構造を作成
            Directory.CreateDirectory(testDir);
            Directory.CreateDirectory(homeDir);
            Directory.CreateDirectory(Path.Combine(testDir, "subdir"));
            Directory.CreateDirectory(Path.Combine(testDir, ".hidden"));

            // テストファイルを作成
            File.WriteAllText(Path.Combine(testDir, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(testDir, "file2.txt"), "content2content2");
            File.WriteAllText(Path.Combine(testDir, ".hiddenfile"), "hidden");
            File.WriteAllText(Path.Combine(testDir, "subdir", "nested.txt"), "nested");

            terminal = new Terminal(homeDir, testDir, registerBuiltInCommands: true);
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

        #region pwd テスト

        // PWD-001 基本動作
        [Test]
        public async Task Pwd_Basic_OutputsWorkingDirectory()
        {
            var exitCode = await terminal.ExecuteAsync("pwd", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Trim().EndsWith("UniTerminalFsTests"));
        }

        // PWD-002 論理パス明示
        [Test]
        public async Task Pwd_LogicalOption_OutputsLogicalPath()
        {
            var exitCode = await terminal.ExecuteAsync("pwd -L", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains(testDir));
        }

        // PWD-003 物理パス
        [Test]
        public async Task Pwd_PhysicalOption_OutputsPhysicalPath()
        {
            var exitCode = await terminal.ExecuteAsync("pwd -P", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Trim().Length > 0);
        }

        // PWD-040 パイプ出力
        [Test]
        public async Task Pwd_Pipe_WorksCorrectly()
        {
            var exitCode = await terminal.ExecuteAsync("pwd | cat", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains(testDir));
        }

        #endregion

        #region cd テスト

        // CD-001 絶対パス移動
        [Test]
        public async Task Cd_AbsolutePath_ChangesDirectory()
        {
            var subdir = PathUtility.NormalizeToSlash(Path.Combine(testDir, "subdir"));

            var exitCode = await terminal.ExecuteAsync($"cd \"{subdir}\"", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(subdir, terminal.WorkingDirectory);
        }

        // CD-002 相対パス移動
        [Test]
        public async Task Cd_RelativePath_ChangesDirectory()
        {
            var exitCode = await terminal.ExecuteAsync("cd subdir", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(terminal.WorkingDirectory.EndsWith("subdir"));
        }

        // CD-003 親ディレクトリ移動
        [Test]
        public async Task Cd_ParentDirectory_ChangesDirectory()
        {
            // まずsubdirに移動
            await terminal.ExecuteAsync("cd subdir", stdout, stderr);
            stdout.Clear();

            // 親ディレクトリに移動
            var exitCode = await terminal.ExecuteAsync("cd ..", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(testDir, terminal.WorkingDirectory);
        }

        // CD-004 ホームディレクトリ移動（引数なし）
        [Test]
        public async Task Cd_NoArgument_ChangesToHome()
        {
            var exitCode = await terminal.ExecuteAsync("cd", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(homeDir, terminal.WorkingDirectory);
        }

        // CD-005 チルダ展開
        [Test]
        public async Task Cd_Tilde_ChangesToHome()
        {
            var exitCode = await terminal.ExecuteAsync("cd ~", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(homeDir, terminal.WorkingDirectory);
        }

        // CD-010 前のディレクトリに移動
        [Test]
        public async Task Cd_Dash_ChangesToPrevious()
        {
            // subdir に移動
            await terminal.ExecuteAsync("cd subdir", stdout, stderr);
            stdout.Clear();

            // 前のディレクトリに戻る
            var exitCode = await terminal.ExecuteAsync("cd -", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(testDir, terminal.WorkingDirectory);
            // cd - は移動先を出力
            Assert.IsTrue(stdout.ToString().Contains(testDir));
        }

        // CD-012 OLDPWD未設定
        [Test]
        public async Task Cd_DashWithNoOldpwd_ReturnsError()
        {
            // 新しいTerminalを作成（前のディレクトリなし）
            var terminal = new Terminal(homeDir, testDir, registerBuiltInCommands: true);

            var exitCode = await terminal.ExecuteAsync("cd -", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("OLDPWD not set"));
        }

        // CD-030 存在しないディレクトリ
        [Test]
        public async Task Cd_NonExistentDirectory_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("cd nonexistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file or directory"));
        }

        // CD-031 ファイルを指定
        [Test]
        public async Task Cd_FileInsteadOfDirectory_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("cd file1.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("Not a directory"));
        }

        // CD-032 引数過多
        [Test]
        public async Task Cd_TooManyArguments_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("cd a b", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("too many arguments"));
        }

        // CD-040 cd → pwd
        [Test]
        public async Task Cd_ThenPwd_ShowsNewDirectory()
        {
            await terminal.ExecuteAsync("cd subdir", stdout, stderr);
            stdout.Clear();

            var exitCode = await terminal.ExecuteAsync("pwd", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("subdir"));
        }

        #endregion

        #region ls テスト

        // LS-001 カレントディレクトリ一覧
        [Test]
        public async Task Ls_Basic_ListsCurrentDirectory()
        {
            var exitCode = await terminal.ExecuteAsync("ls", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.txt"));
            Assert.IsTrue(output.Contains("subdir"));
        }

        // LS-002 指定ディレクトリ一覧
        [Test]
        public async Task Ls_SpecificDirectory_ListsContents()
        {
            var exitCode = await terminal.ExecuteAsync("ls subdir", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("nested.txt"));
        }

        // LS-003 隠しファイル表示
        [Test]
        public async Task Ls_AllOption_ShowsHiddenFiles()
        {
            var exitCode = await terminal.ExecuteAsync("ls -a", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains(".hiddenfile"));
            Assert.IsTrue(output.Contains(".hidden"));
        }

        // LS-004 詳細形式
        [Test]
        public async Task Ls_LongFormat_ShowsDetails()
        {
            var exitCode = await terminal.ExecuteAsync("ls -l", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // パーミッション形式を含むか確認
            Assert.IsTrue(output.Contains("rw"));
            // 日時形式を含むか確認
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(output, @"\d{4}-\d{2}-\d{2}"));
        }

        // LS-010 詳細+人間可読サイズ
        [Test]
        public async Task Ls_LongHumanReadable_ShowsHumanSize()
        {
            var exitCode = await terminal.ExecuteAsync("ls -lh", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // サイズにBまたはKが含まれる
            Assert.IsTrue(output.Contains("B") || output.Contains("K"));
        }

        // LS-012 逆順ソート
        [Test]
        public async Task Ls_ReverseOption_ReversesOrder()
        {
            var exitCode1 = await terminal.ExecuteAsync("ls -l", stdout, stderr);
            var normalOutput = stdout.ToString();
            stdout.Clear();

            var exitCode2 = await terminal.ExecuteAsync("ls -lr", stdout, stderr);
            var reversedOutput = stdout.ToString();

            Assert.AreEqual(ExitCode.Success, exitCode1);
            Assert.AreEqual(ExitCode.Success, exitCode2);
            Assert.AreNotEqual(normalOutput, reversedOutput);
        }

        // LS-020 存在しないパス
        [Test]
        public async Task Ls_NonExistentPath_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("ls nonexistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("No such file or directory"));
        }

        // LS-021 ファイル指定
        [Test]
        public async Task Ls_FileArgument_ShowsFileInfo()
        {
            var exitCode = await terminal.ExecuteAsync("ls -l file1.txt", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("file1.txt"));
        }

        // LS-030 再帰表示
        [Test]
        public async Task Ls_Recursive_ShowsSubdirectories()
        {
            var exitCode = await terminal.ExecuteAsync("ls -R", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("subdir"));
            Assert.IsTrue(output.Contains("nested.txt"));
        }

        // LS + パイプ
        [Test]
        public async Task Ls_Pipe_WorksCorrectly()
        {
            var exitCode = await terminal.ExecuteAsync("ls | grep --pattern=file", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.txt"));
        }

        // 隠しファイルがデフォルトで非表示
        [Test]
        public async Task Ls_Default_HidesHiddenFiles()
        {
            var exitCode = await terminal.ExecuteAsync("ls", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains(".hiddenfile"));
            // .hidden は非詳細形式では "/" なしで表示される
            Assert.IsFalse(output.Contains(".hidden"));
        }

        #endregion
    }
}
