using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// logコマンドのテスト。
    /// </summary>
    public class LogCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;

        [SetUp]
        public void SetUp()
        {
            terminal = new Terminal(registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
        }

        [TearDown]
        public void TearDown()
        {
            terminal?.Dispose();
        }

        #region 基本動作テスト

        // LOG-001 基本動作
        [Test]
        public async Task Log_Basic_ShowsLogs()
        {
            // ログを出力
            Debug.Log("Test Info Message");

            // 少し待ってログバッファにエントリが追加されるのを待つ
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("[INFO]"));
            Assert.IsTrue(output.Contains("Test Info Message"));
        }

        // LOG-002 Warningログ
        [Test]
        public async Task Log_Warning_ShowsWarning()
        {
            LogAssert.Expect(LogType.Warning, "Test Warning Message");
            Debug.LogWarning("Test Warning Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("[WARN]"));
            Assert.IsTrue(output.Contains("Test Warning Message"));
        }

        // LOG-003 Errorログ
        [Test]
        public async Task Log_Error_ShowsError()
        {
            LogAssert.Expect(LogType.Error, "Test Error Message");
            Debug.LogError("Test Error Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("[ERROR]"));
            Assert.IsTrue(output.Contains("Test Error Message"));
        }

        #endregion

        #region フィルタテスト

        // LOG-010 Infoフィルタ
        [Test]
        public async Task Log_InfoFilter_ShowsOnlyInfo()
        {
            LogAssert.Expect(LogType.Warning, "Warning Message");
            LogAssert.Expect(LogType.Error, "Error Message");

            Debug.Log("Info Message");
            Debug.LogWarning("Warning Message");
            Debug.LogError("Error Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -i", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("[INFO]"));
            Assert.IsFalse(output.Contains("[WARN]"));
            Assert.IsFalse(output.Contains("[ERROR]"));
        }

        // LOG-011 Warningフィルタ
        [Test]
        public async Task Log_WarnFilter_ShowsOnlyWarning()
        {
            LogAssert.Expect(LogType.Warning, "Warning Message");
            LogAssert.Expect(LogType.Error, "Error Message");

            Debug.Log("Info Message");
            Debug.LogWarning("Warning Message");
            Debug.LogError("Error Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -w", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("[INFO]"));
            Assert.IsTrue(output.Contains("[WARN]"));
            Assert.IsFalse(output.Contains("[ERROR]"));
        }

        // LOG-012 Errorフィルタ
        [Test]
        public async Task Log_ErrorFilter_ShowsOnlyErrors()
        {
            LogAssert.Expect(LogType.Warning, "Warning Message");
            LogAssert.Expect(LogType.Error, "Error Message");

            Debug.Log("Info Message");
            Debug.LogWarning("Warning Message");
            Debug.LogError("Error Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -e", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("[INFO]"));
            Assert.IsFalse(output.Contains("[WARN]"));
            Assert.IsTrue(output.Contains("[ERROR]"));
        }

        // LOG-013 複合フィルタ
        [Test]
        public async Task Log_MultipleFilters_ShowsMatching()
        {
            LogAssert.Expect(LogType.Warning, "Warning Message");
            LogAssert.Expect(LogType.Error, "Error Message");

            Debug.Log("Info Message");
            Debug.LogWarning("Warning Message");
            Debug.LogError("Error Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -i -w", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("[INFO]"));
            Assert.IsTrue(output.Contains("[WARN]"));
            Assert.IsFalse(output.Contains("[ERROR]"));
        }

        #endregion

        #region head/tailテスト

        // LOG-020 tailオプション
        [Test]
        public async Task Log_TailOption_ShowsLastN()
        {
            for (int i = 1; i <= 5; i++)
                Debug.Log($"Message {i}");

            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -t 2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsFalse(output.Contains("Message 3"));
            Assert.IsTrue(output.Contains("Message 4"));
            Assert.IsTrue(output.Contains("Message 5"));
        }

        // LOG-021 headオプション
        [Test]
        public async Task Log_HeadOption_ShowsFirstN()
        {
            for (int i = 1; i <= 5; i++)
                Debug.Log($"Message {i}");

            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -h 2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Message 1"));
            Assert.IsTrue(output.Contains("Message 2"));
            Assert.IsFalse(output.Contains("Message 3"));
        }

        // LOG-022 head と tail の排他
        [Test]
        public async Task Log_BothHeadAndTail_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("log -h 5 -t 5", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("cannot specify both"));
        }

        #endregion

        #region スタックトレーステスト

        // LOG-030 スタックトレースオプション
        [Test]
        public async Task Log_StackTraceOption_ShowsStackTrace()
        {
            LogAssert.Expect(LogType.Error, "Error with stack trace");
            Debug.LogError("Error with stack trace");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -s -e", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("[ERROR]"));
            // スタックトレースはインデントされて表示される
            Assert.IsTrue(output.Contains("    "));
        }

        #endregion

        #region 色付け確認テスト

        // LOG-040 色付け（Info）
        [Test]
        public async Task Log_InfoColor_UsesWhite()
        {
            Debug.Log("Info Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -i", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("<color=white>"));
        }

        // LOG-041 色付け（Warning）
        [Test]
        public async Task Log_WarningColor_UsesYellow()
        {
            LogAssert.Expect(LogType.Warning, "Warning Message");
            Debug.LogWarning("Warning Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -w", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("<color=yellow>"));
        }

        // LOG-042 色付け（Error）
        [Test]
        public async Task Log_ErrorColor_UsesRed()
        {
            LogAssert.Expect(LogType.Error, "Error Message");
            Debug.LogError("Error Message");
            await Task.Yield();

            var exitCode = await terminal.ExecuteAsync("log -e", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("<color=red>"));
        }

        #endregion
    }
}
