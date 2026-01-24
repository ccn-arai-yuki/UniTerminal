#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading.Tasks;
using NUnit.Framework;
using Xeon.UniTerminal;
using Xeon.UniTerminal.UniTask;

namespace Xeon.UniTerminal.Tests
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTaskを使用したTerminal統合テスト。
    /// </summary>
    [TestFixture]
    public class UniTaskIntegrationTests
    {
        private Terminal terminal;
        private string testDir;

        [SetUp]
        public void SetUp()
        {
            // パスをスラッシュで正規化（Terminal内部でスラッシュに統一されるため）
            testDir = PathUtility.NormalizeToSlash(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "UniTerminalUniTaskTest"));
            if (!System.IO.Directory.Exists(testDir))
            {
                System.IO.Directory.CreateDirectory(testDir);
            }
            terminal = new Terminal(testDir, testDir, registerBuiltInCommands: true);
        }

        [TearDown]
        public void TearDown()
        {
            if (System.IO.Directory.Exists(testDir))
            {
                try
                {
                    System.IO.Directory.Delete(testDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #region Basic Execution Tests

        [Test]
        public async Task ExecuteUniTaskAsync_Echo_OutputsText()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("echo Hello World", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("Hello World"));
        }

        [Test]
        public async Task ExecuteUniTaskAsync_Help_ShowsCommands()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("help", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("echo"));
            Assert.IsTrue(stdout.ToString().Contains("help"));
        }

        [Test]
        public async Task ExecuteUniTaskAsync_Pwd_ShowsWorkingDirectory()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("pwd", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains(testDir));
        }

        [Test]
        public async Task ExecuteUniTaskAsync_EmptyInput_ReturnsSuccess()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        [Test]
        public async Task ExecuteUniTaskAsync_WhitespaceInput_ReturnsSuccess()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("   ", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
        }

        #endregion

        #region Pipeline Tests

        [Test]
        public async Task ExecuteUniTaskAsync_Pipeline_PassesOutputToNextCommand()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("echo hello | grep --pattern=hello", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("hello"));
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task ExecuteUniTaskAsync_UnknownCommand_ReturnsUsageError()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("unknowncommand", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
        }

        [Test]
        public async Task ExecuteUniTaskAsync_InvalidSyntax_ReturnsUsageError()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            // Unclosed quote
            var exitCode = await terminal.ExecuteUniTaskAsync("echo \"unclosed", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
        }

        #endregion

        #region History Tests

        [Test]
        public async Task ExecuteUniTaskAsync_AddsToHistory()
        {
            var stdout = new UniTaskStringBuilderTextWriter();
            var stderr = new UniTaskStringBuilderTextWriter();

            await terminal.ExecuteUniTaskAsync("echo test", stdout, stderr);

            Assert.AreEqual(1, terminal.CommandHistory.Count);
            Assert.AreEqual("echo test", terminal.CommandHistory[0]);
        }

        #endregion

        #region Task Adapter Overload Tests

        [Test]
        public async Task ExecuteUniTaskAsync_WithTaskWriters_Works()
        {
            var stdout = new StringBuilderTextWriter();
            var stderr = new StringBuilderTextWriter();

            var exitCode = await terminal.ExecuteUniTaskAsync("echo Adapter Test", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("Adapter Test"));
        }

        #endregion
    }
}
#endif
