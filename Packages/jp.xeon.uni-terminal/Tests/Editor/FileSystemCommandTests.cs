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
        private Terminal _terminal;
        private StringBuilderTextWriter _stdout;
        private StringBuilderTextWriter _stderr;
        private string _testDir;
        private string _homeDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "UniTerminalFsTests");
            _homeDir = Path.Combine(_testDir, "home");

            // テストディレクトリ構造を作成
            Directory.CreateDirectory(_testDir);
            Directory.CreateDirectory(_homeDir);
            Directory.CreateDirectory(Path.Combine(_testDir, "subdir"));
            Directory.CreateDirectory(Path.Combine(_testDir, ".hidden"));

            // テストファイルを作成
            File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(_testDir, "file2.txt"), "content2content2");
            File.WriteAllText(Path.Combine(_testDir, ".hiddenfile"), "hidden");
            File.WriteAllText(Path.Combine(_testDir, "subdir", "nested.txt"), "nested");

            _terminal = new Terminal(_homeDir, _testDir, registerBuiltInCommands: true);
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

        #region pwd テスト

        // PWD-001 基本動作
        [Test]
        public async Task Pwd_Basic_OutputsWorkingDirectory()
        {
            var exitCode = await _terminal.ExecuteAsync("pwd", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Trim().EndsWith("UniTerminalFsTests"));
        }

        // PWD-002 論理パス明示
        [Test]
        public async Task Pwd_LogicalOption_OutputsLogicalPath()
        {
            var exitCode = await _terminal.ExecuteAsync("pwd -L", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains(_testDir));
        }

        // PWD-003 物理パス
        [Test]
        public async Task Pwd_PhysicalOption_OutputsPhysicalPath()
        {
            var exitCode = await _terminal.ExecuteAsync("pwd -P", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Trim().Length > 0);
        }

        // PWD-040 パイプ出力
        [Test]
        public async Task Pwd_Pipe_WorksCorrectly()
        {
            var exitCode = await _terminal.ExecuteAsync("pwd | cat", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains(_testDir));
        }

        #endregion

        #region cd テスト

        // CD-001 絶対パス移動
        [Test]
        public async Task Cd_AbsolutePath_ChangesDirectory()
        {
            var subdir = Path.Combine(_testDir, "subdir");

            var exitCode = await _terminal.ExecuteAsync($"cd \"{subdir}\"", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(subdir, _terminal.WorkingDirectory);
        }

        // CD-002 相対パス移動
        [Test]
        public async Task Cd_RelativePath_ChangesDirectory()
        {
            var exitCode = await _terminal.ExecuteAsync("cd subdir", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_terminal.WorkingDirectory.EndsWith("subdir"));
        }

        // CD-003 親ディレクトリ移動
        [Test]
        public async Task Cd_ParentDirectory_ChangesDirectory()
        {
            // まずsubdirに移動
            await _terminal.ExecuteAsync("cd subdir", _stdout, _stderr);
            _stdout.Clear();

            // 親ディレクトリに移動
            var exitCode = await _terminal.ExecuteAsync("cd ..", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(_testDir, _terminal.WorkingDirectory);
        }

        // CD-004 ホームディレクトリ移動（引数なし）
        [Test]
        public async Task Cd_NoArgument_ChangesToHome()
        {
            var exitCode = await _terminal.ExecuteAsync("cd", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(_homeDir, _terminal.WorkingDirectory);
        }

        // CD-005 チルダ展開
        [Test]
        public async Task Cd_Tilde_ChangesToHome()
        {
            var exitCode = await _terminal.ExecuteAsync("cd ~", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(_homeDir, _terminal.WorkingDirectory);
        }

        // CD-010 前のディレクトリに移動
        [Test]
        public async Task Cd_Dash_ChangesToPrevious()
        {
            // subdir に移動
            await _terminal.ExecuteAsync("cd subdir", _stdout, _stderr);
            _stdout.Clear();

            // 前のディレクトリに戻る
            var exitCode = await _terminal.ExecuteAsync("cd -", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(_testDir, _terminal.WorkingDirectory);
            // cd - は移動先を出力
            Assert.IsTrue(_stdout.ToString().Contains(_testDir));
        }

        // CD-012 OLDPWD未設定
        [Test]
        public async Task Cd_DashWithNoOldpwd_ReturnsError()
        {
            // 新しいTerminalを作成（前のディレクトリなし）
            var terminal = new Terminal(_homeDir, _testDir, registerBuiltInCommands: true);

            var exitCode = await terminal.ExecuteAsync("cd -", _stdout, _stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("OLDPWD not set"));
        }

        // CD-030 存在しないディレクトリ
        [Test]
        public async Task Cd_NonExistentDirectory_ReturnsError()
        {
            var exitCode = await _terminal.ExecuteAsync("cd nonexistent", _stdout, _stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("No such file or directory"));
        }

        // CD-031 ファイルを指定
        [Test]
        public async Task Cd_FileInsteadOfDirectory_ReturnsError()
        {
            var exitCode = await _terminal.ExecuteAsync("cd file1.txt", _stdout, _stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("Not a directory"));
        }

        // CD-032 引数過多
        [Test]
        public async Task Cd_TooManyArguments_ReturnsError()
        {
            var exitCode = await _terminal.ExecuteAsync("cd a b", _stdout, _stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("too many arguments"));
        }

        // CD-040 cd → pwd
        [Test]
        public async Task Cd_ThenPwd_ShowsNewDirectory()
        {
            await _terminal.ExecuteAsync("cd subdir", _stdout, _stderr);
            _stdout.Clear();

            var exitCode = await _terminal.ExecuteAsync("pwd", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("subdir"));
        }

        #endregion

        #region ls テスト

        // LS-001 カレントディレクトリ一覧
        [Test]
        public async Task Ls_Basic_ListsCurrentDirectory()
        {
            var exitCode = await _terminal.ExecuteAsync("ls", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.txt"));
            Assert.IsTrue(output.Contains("subdir"));
        }

        // LS-002 指定ディレクトリ一覧
        [Test]
        public async Task Ls_SpecificDirectory_ListsContents()
        {
            var exitCode = await _terminal.ExecuteAsync("ls subdir", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("nested.txt"));
        }

        // LS-003 隠しファイル表示
        [Test]
        public async Task Ls_AllOption_ShowsHiddenFiles()
        {
            var exitCode = await _terminal.ExecuteAsync("ls -a", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains(".hiddenfile"));
            Assert.IsTrue(output.Contains(".hidden"));
        }

        // LS-004 詳細形式
        [Test]
        public async Task Ls_LongFormat_ShowsDetails()
        {
            var exitCode = await _terminal.ExecuteAsync("ls -l", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            // パーミッション形式を含むか確認
            Assert.IsTrue(output.Contains("rw"));
            // 日時形式を含むか確認
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(output, @"\d{4}-\d{2}-\d{2}"));
        }

        // LS-010 詳細+人間可読サイズ
        [Test]
        public async Task Ls_LongHumanReadable_ShowsHumanSize()
        {
            var exitCode = await _terminal.ExecuteAsync("ls -lh", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            // サイズにBまたはKが含まれる
            Assert.IsTrue(output.Contains("B") || output.Contains("K"));
        }

        // LS-012 逆順ソート
        [Test]
        public async Task Ls_ReverseOption_ReversesOrder()
        {
            var exitCode1 = await _terminal.ExecuteAsync("ls -l", _stdout, _stderr);
            var normalOutput = _stdout.ToString();
            _stdout.Clear();

            var exitCode2 = await _terminal.ExecuteAsync("ls -lr", _stdout, _stderr);
            var reversedOutput = _stdout.ToString();

            Assert.AreEqual(ExitCode.Success, exitCode1);
            Assert.AreEqual(ExitCode.Success, exitCode2);
            Assert.AreNotEqual(normalOutput, reversedOutput);
        }

        // LS-020 存在しないパス
        [Test]
        public async Task Ls_NonExistentPath_ReturnsError()
        {
            var exitCode = await _terminal.ExecuteAsync("ls nonexistent", _stdout, _stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(_stderr.ToString().Contains("No such file or directory"));
        }

        // LS-021 ファイル指定
        [Test]
        public async Task Ls_FileArgument_ShowsFileInfo()
        {
            var exitCode = await _terminal.ExecuteAsync("ls -l file1.txt", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(_stdout.ToString().Contains("file1.txt"));
        }

        // LS-030 再帰表示
        [Test]
        public async Task Ls_Recursive_ShowsSubdirectories()
        {
            var exitCode = await _terminal.ExecuteAsync("ls -R", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains("subdir"));
            Assert.IsTrue(output.Contains("nested.txt"));
        }

        // LS + パイプ
        [Test]
        public async Task Ls_Pipe_WorksCorrectly()
        {
            var exitCode = await _terminal.ExecuteAsync("ls | grep --pattern=file", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsTrue(output.Contains("file1.txt"));
            Assert.IsTrue(output.Contains("file2.txt"));
        }

        // 隠しファイルがデフォルトで非表示
        [Test]
        public async Task Ls_Default_HidesHiddenFiles()
        {
            var exitCode = await _terminal.ExecuteAsync("ls", _stdout, _stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = _stdout.ToString();
            Assert.IsFalse(output.Contains(".hiddenfile"));
            // .hidden は非詳細形式では "/" なしで表示される
            Assert.IsFalse(output.Contains(".hidden"));
        }

        #endregion
    }
}
