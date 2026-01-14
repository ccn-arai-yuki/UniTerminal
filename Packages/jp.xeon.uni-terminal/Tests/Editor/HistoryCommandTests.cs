using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// historyコマンドのテスト。
    /// </summary>
    public class HistoryCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            testDir = Path.Combine(Path.GetTempPath(), "UniTerminalHistoryTests");
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

        // HIST-001 履歴表示
        [Test]
        public async Task History_Default_ShowsHistory()
        {
            // いくつかコマンドを実行
            await terminal.ExecuteAsync("echo first", stdout, stderr);
            await terminal.ExecuteAsync("echo second", stdout, stderr);
            await terminal.ExecuteAsync("echo third", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("echo first"));
            Assert.IsTrue(output.Contains("echo second"));
            Assert.IsTrue(output.Contains("echo third"));
        }

        // HIST-002 履歴番号表示
        [Test]
        public async Task History_ShowsLineNumbers()
        {
            await terminal.ExecuteAsync("echo test", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // 行番号が含まれていることを確認（数字で始まる）
            Assert.IsTrue(output.Contains("1"));
        }

        // HIST-003 -n オプション
        [Test]
        public async Task History_Number_LimitsOutput()
        {
            await terminal.ExecuteAsync("echo first", stdout, stderr);
            await terminal.ExecuteAsync("echo second", stdout, stderr);
            await terminal.ExecuteAsync("echo third", stdout, stderr);
            await terminal.ExecuteAsync("echo fourth", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history -n 2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // 最後の2件のみ表示
            Assert.IsFalse(output.Contains("echo first"));
            Assert.IsFalse(output.Contains("echo second"));
            Assert.IsTrue(output.Contains("echo third"));
            Assert.IsTrue(output.Contains("echo fourth"));
        }

        // HIST-004 -r オプション
        [Test]
        public async Task History_Reverse_ShowsReverseOrder()
        {
            await terminal.ExecuteAsync("echo first", stdout, stderr);
            await terminal.ExecuteAsync("echo second", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history -r", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // secondがfirstより前に表示される
            int secondIndex = output.IndexOf("echo second");
            int firstIndex = output.IndexOf("echo first");
            Assert.IsTrue(secondIndex < firstIndex);
        }

        // HIST-010 -c オプション
        [Test]
        public async Task History_Clear_ClearsAllHistory()
        {
            await terminal.ExecuteAsync("echo first", stdout, stderr);
            await terminal.ExecuteAsync("echo second", stdout, stderr);
            await terminal.ExecuteAsync("history -c", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            // history -c後にhistoryを実行しているのでhistoryコマンドだけが履歴にある
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("echo first"));
            Assert.IsFalse(output.Contains("echo second"));
        }

        // HIST-011 -d オプション
        [Test]
        public async Task History_Delete_RemovesSpecificEntry()
        {
            await terminal.ExecuteAsync("echo first", stdout, stderr);
            await terminal.ExecuteAsync("echo second", stdout, stderr);
            await terminal.ExecuteAsync("echo third", stdout, stderr);

            // 2番目のエントリを削除
            await terminal.ExecuteAsync("history -d 2", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("echo first"));
            Assert.IsFalse(output.Contains("echo second"));
            Assert.IsTrue(output.Contains("echo third"));
        }

        // HIST-012 -d 無効な番号
        [Test]
        public async Task History_Delete_InvalidPosition_ReturnsError()
        {
            await terminal.ExecuteAsync("echo test", stdout, stderr);

            stderr = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history -d 999", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("out of range"));
        }

        // HIST-020 重複コマンドの処理
        [Test]
        public async Task History_DuplicateCommands_NotAdded()
        {
            await terminal.ExecuteAsync("echo same", stdout, stderr);
            await terminal.ExecuteAsync("echo same", stdout, stderr);
            await terminal.ExecuteAsync("echo same", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // 連続した同じコマンドは1回だけ記録される
            int count = 0;
            int index = 0;
            while ((index = output.IndexOf("echo same", index)) != -1)
            {
                count++;
                index++;
            }
            Assert.AreEqual(1, count);
        }

        // HIST-030 パイプ連携
        [Test]
        public async Task History_Pipe_WorksWithGrep()
        {
            await terminal.ExecuteAsync("echo apple", stdout, stderr);
            await terminal.ExecuteAsync("echo banana", stdout, stderr);
            await terminal.ExecuteAsync("echo apricot", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("history | grep --pattern=apple", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("echo apple"));
            Assert.IsFalse(output.Contains("echo banana"));
        }

        // HIST-040 空の履歴
        [Test]
        public async Task History_Empty_ReturnsSuccess()
        {
            // 新しいターミナルを作成（履歴なし）
            var newTerminal = new Terminal(testDir, testDir, registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();

            // 履歴を即座にクリア
            newTerminal.ClearHistory();

            var exitCode = await newTerminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        // HIST-050 履歴の最大サイズ
        [Test]
        public async Task History_MaxSize_LimitsEntries()
        {
            // 最大3件の履歴を持つターミナルを作成
            var smallTerminal = new Terminal(testDir, testDir, registerBuiltInCommands: true, maxHistorySize: 3);

            await smallTerminal.ExecuteAsync("echo first", stdout, stderr);
            await smallTerminal.ExecuteAsync("echo second", stdout, stderr);
            await smallTerminal.ExecuteAsync("echo third", stdout, stderr);
            await smallTerminal.ExecuteAsync("echo fourth", stdout, stderr);

            stdout = new StringBuilderTextWriter();
            var exitCode = await smallTerminal.ExecuteAsync("history", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // 最初のエントリは削除されているはず
            Assert.IsFalse(output.Contains("echo first"));
            Assert.IsTrue(output.Contains("echo second"));
            Assert.IsTrue(output.Contains("echo third"));
            Assert.IsTrue(output.Contains("echo fourth"));
        }
    }
}
