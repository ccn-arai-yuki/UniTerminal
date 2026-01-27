using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// sceneコマンドのPlayModeテスト
    /// </summary>
    public class SceneCommandPlayModeTests
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

        [UnityTest]
        public IEnumerator Scene_List_ShowsLoadedScenes()
        {
            yield return null;

            var task = terminal.ExecuteAsync("scene list", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Length > 0 || stderr.ToString().Length == 0);
        }

        [UnityTest]
        public IEnumerator Scene_List_Long_ShowsDetailedInfo()
        {
            yield return null;

            var task = terminal.ExecuteAsync("scene list -l", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
        }

        [UnityTest]
        public IEnumerator Scene_Active_ShowsActiveScene()
        {
            yield return null;

            var activeScene = SceneManager.GetActiveScene();

            var task = terminal.ExecuteAsync("scene active", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Active scene:"));
            Assert.IsTrue(output.Contains(activeScene.name));
        }

        [UnityTest]
        public IEnumerator Scene_Info_ShowsSceneDetails()
        {
            yield return null;

            var task = terminal.ExecuteAsync("scene info", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Scene:"));
            Assert.IsTrue(output.Contains("Path:"));
            Assert.IsTrue(output.Contains("Is Loaded:"));
            Assert.IsTrue(output.Contains("Root Count:"));
        }

        [UnityTest]
        public IEnumerator Scene_Info_WithRootObjects_ShowsRootList()
        {
            var testObj = new GameObject("SceneCommand_TestObject");

            yield return null;

            var task = terminal.ExecuteAsync("scene info", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Root Objects:"));

            Object.Destroy(testObj);
        }

        [UnityTest]
        public IEnumerator Scene_Unload_OnlyLoadedScene_ReturnsError()
        {
            yield return null;

            var activeScene = SceneManager.GetActiveScene();

            var task = terminal.ExecuteAsync($"scene unload {activeScene.name}", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            var error = stderr.ToString();
            Assert.IsTrue(error.Contains("cannot unload"));
        }

        [UnityTest]
        public IEnumerator Scene_Active_ChangeScene_NotLoaded_ReturnsError()
        {
            yield return null;

            var task = terminal.ExecuteAsync("scene active NonExistentScene", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not loaded"));
        }

        [UnityTest]
        public IEnumerator Scene_Load_InvalidScene_ReturnsError()
        {
            yield return null;

            var task = terminal.ExecuteAsync("scene load InvalidSceneName123", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        [UnityTest]
        public IEnumerator Scene_Unload_NotLoadedScene_ReturnsError()
        {
            yield return null;

            var task = terminal.ExecuteAsync("scene unload NotLoadedScene", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not loaded"));
        }
    }
}
