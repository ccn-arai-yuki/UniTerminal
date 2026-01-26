using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xeon.UniTerminal.Tests.Editor
{
    /// <summary>
    /// sceneコマンドのEditorテスト
    /// </summary>
    public class SceneCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;

        [SetUp]
        public void SetUp()
        {
            terminal = new Terminal("/", "/", registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
        }

        [Test]
        public async Task Scene_NoSubcommand_ReturnsUsageError()
        {
            var result = await terminal.ExecuteAsync("scene", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, result);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        [Test]
        public async Task Scene_UnknownSubcommand_ReturnsUsageError()
        {
            var result = await terminal.ExecuteAsync("scene unknown", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, result);
            Assert.IsTrue(stderr.ToString().Contains("unknown subcommand"));
        }

        [Test]
        public async Task Scene_List_ReturnsSuccess()
        {
            var result = await terminal.ExecuteAsync("scene list", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, result);
        }

        [Test]
        public async Task Scene_List_Long_ReturnsSuccess()
        {
            var result = await terminal.ExecuteAsync("scene list -l", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, result);
        }

        [Test]
        public async Task Scene_List_All_ReturnsSuccess()
        {
            var result = await terminal.ExecuteAsync("scene list -a", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, result);
        }

        [Test]
        public async Task Scene_Active_ShowsCurrentScene()
        {
            var result = await terminal.ExecuteAsync("scene active", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, result);
            Assert.IsTrue(stdout.ToString().Contains("Active scene:"));
        }

        [Test]
        public async Task Scene_Info_ShowsSceneInfo()
        {
            var result = await terminal.ExecuteAsync("scene info", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Scene:"));
            Assert.IsTrue(output.Contains("Path:"));
            Assert.IsTrue(output.Contains("Build Index:"));
        }

        [Test]
        public async Task Scene_Load_MissingArgument_ReturnsUsageError()
        {
            var result = await terminal.ExecuteAsync("scene load", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, result);
            Assert.IsTrue(stderr.ToString().Contains("missing scene name"));
        }

        [Test]
        public async Task Scene_Load_InvalidScene_ReturnsRuntimeError()
        {
            var result = await terminal.ExecuteAsync("scene load NonExistentScene", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, result);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        [Test]
        public async Task Scene_Unload_MissingArgument_ReturnsUsageError()
        {
            var result = await terminal.ExecuteAsync("scene unload", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, result);
            Assert.IsTrue(stderr.ToString().Contains("missing scene name"));
        }

        [Test]
        public async Task Scene_Unload_InvalidScene_ReturnsRuntimeError()
        {
            var result = await terminal.ExecuteAsync("scene unload NonExistentScene", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, result);
            Assert.IsTrue(stderr.ToString().Contains("not loaded"));
        }

        [Test]
        public async Task Scene_Active_InvalidScene_ReturnsRuntimeError()
        {
            var result = await terminal.ExecuteAsync("scene active NonExistentScene", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, result);
            Assert.IsTrue(stderr.ToString().Contains("not loaded"));
        }

        [Test]
        public async Task Scene_Info_InvalidScene_ReturnsRuntimeError()
        {
            var result = await terminal.ExecuteAsync("scene info NonExistentScene", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, result);
            Assert.IsTrue(stderr.ToString().Contains("not loaded"));
        }

        [Test]
        public async Task Scene_Create_MissingArgument_ReturnsUsageError()
        {
            var result = await terminal.ExecuteAsync("scene create", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, result);
            Assert.IsTrue(stderr.ToString().Contains("missing scene name"));
        }
    }
}
