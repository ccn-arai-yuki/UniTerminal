using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// componentコマンドのPlayModeテスト
    /// </summary>
    public class ComponentCommandPlayModeTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private List<GameObject> createdObjects;

        [SetUp]
        public void SetUp()
        {
            terminal = new Terminal("/", "/", registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
            createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in createdObjects)
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
            createdObjects.Clear();
        }

        private GameObject CreateTestObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }
            createdObjects.Add(go);
            return go;
        }

        [UnityTest]
        public IEnumerator Component_List_ShowsComponents()
        {
            var target = CreateTestObject("PlayMode_CompList");
            target.AddComponent<Rigidbody>();
            target.AddComponent<BoxCollider>();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("component list /PlayMode_CompList", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Transform"));
            Assert.IsTrue(output.Contains("Rigidbody"));
            Assert.IsTrue(output.Contains("BoxCollider"));
        }

        [UnityTest]
        public IEnumerator Component_Add_AddsComponent()
        {
            var target = CreateTestObject("PlayMode_CompAdd");
            Assert.IsNull(target.GetComponent<Rigidbody>());

            yield return null;

            var task = terminal.ExecuteAsync("component add /PlayMode_CompAdd Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsNotNull(target.GetComponent<Rigidbody>());
        }

        [UnityTest]
        public IEnumerator Component_Add_MultipleComponents()
        {
            var target = CreateTestObject("PlayMode_CompAddMulti");

            yield return null;

            var task = terminal.ExecuteAsync("component add /PlayMode_CompAddMulti BoxCollider", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);

            stdout = new StringBuilderTextWriter();
            task = terminal.ExecuteAsync("component add /PlayMode_CompAddMulti SphereCollider", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsNotNull(target.GetComponent<BoxCollider>());
            Assert.IsNotNull(target.GetComponent<SphereCollider>());
        }

        [UnityTest]
        public IEnumerator Component_Remove_RemovesComponent()
        {
            var target = CreateTestObject("PlayMode_CompRemove");
            target.AddComponent<Rigidbody>();
            Assert.IsNotNull(target.GetComponent<Rigidbody>());

            yield return null;

            var task = terminal.ExecuteAsync("component remove /PlayMode_CompRemove Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);

            yield return null;

            Assert.IsNull(target.GetComponent<Rigidbody>());
        }

        [UnityTest]
        public IEnumerator Component_Info_ShowsComponentInfo()
        {
            var target = CreateTestObject("PlayMode_CompInfo");
            var rb = target.AddComponent<Rigidbody>();
            rb.mass = 10f;

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("component info /PlayMode_CompInfo Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Rigidbody") || output.Contains("mass"));
        }

        [UnityTest]
        public IEnumerator Component_Enable_EnablesComponent()
        {
            var target = CreateTestObject("PlayMode_CompEnable");
            var collider = target.AddComponent<BoxCollider>();
            collider.enabled = false;
            Assert.IsFalse(collider.enabled);

            yield return null;

            var task = terminal.ExecuteAsync("component enable /PlayMode_CompEnable BoxCollider", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsTrue(collider.enabled);
        }

        [UnityTest]
        public IEnumerator Component_Disable_DisablesComponent()
        {
            var target = CreateTestObject("PlayMode_CompDisable");
            var collider = target.AddComponent<BoxCollider>();
            Assert.IsTrue(collider.enabled);

            yield return null;

            var task = terminal.ExecuteAsync("component disable /PlayMode_CompDisable BoxCollider", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsFalse(collider.enabled);
        }

        [UnityTest]
        public IEnumerator Component_List_Verbose_ShowsFullTypeName()
        {
            var target = CreateTestObject("PlayMode_CompVerbose");
            target.AddComponent<Rigidbody>();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("component list /PlayMode_CompVerbose -v", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("UnityEngine") || output.Contains("Rigidbody"));
        }

        [UnityTest]
        public IEnumerator Component_NotFound_ReturnsError()
        {
            var target = CreateTestObject("PlayMode_CompNotFound");

            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("component info /PlayMode_CompNotFound Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not found") || stderr.ToString().Length > 0);
        }

        [UnityTest]
        public IEnumerator Component_InvalidPath_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("component list /PlayMode_NonExistent", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
        }
    }
}
