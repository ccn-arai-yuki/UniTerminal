using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// goコマンドのPlayModeテスト
    /// </summary>
    public class GoCommandPlayModeTests
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

            // コマンドで作成されたオブジェクトもクリーンアップ
            var testObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in testObjects)
            {
                if (obj.name.StartsWith("PlayMode_Go"))
                {
                    Object.Destroy(obj);
                }
            }
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
        public IEnumerator Go_Create_CreatesEmptyGameObject()
        {
            yield return null;

            var task = terminal.ExecuteAsync("go create PlayMode_GoCreate", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);

            yield return null;

            var created = GameObject.Find("PlayMode_GoCreate");
            Assert.IsNotNull(created);
        }

        [UnityTest]
        public IEnumerator Go_Create_Primitive_CreatesCube()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("go create PlayMode_GoCube -p Cube", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result, $"stderr: {stderr}");

            yield return null;

            var created = GameObject.Find("PlayMode_GoCube");
            Assert.IsNotNull(created, "GameObject not found after creation");
            Assert.IsNotNull(created.GetComponent<MeshFilter>(), "MeshFilter not found on primitive");
        }

        [UnityTest]
        public IEnumerator Go_Create_WithParent_CreatesAsChild()
        {
            var parent = CreateTestObject("PlayMode_GoParent");

            yield return null;

            var task = terminal.ExecuteAsync("go create PlayMode_GoChild --parent /PlayMode_GoParent", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);

            yield return null;

            var child = GameObject.Find("PlayMode_GoChild");
            Assert.IsNotNull(child);
            Assert.AreEqual(parent.transform, child.transform.parent);
        }

        [UnityTest]
        public IEnumerator Go_Delete_DeletesGameObject()
        {
            var target = CreateTestObject("PlayMode_GoDelete");

            yield return null;

            var task = terminal.ExecuteAsync("go delete /PlayMode_GoDelete", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);

            yield return null;

            var found = GameObject.Find("PlayMode_GoDelete");
            Assert.IsNull(found);
            createdObjects.Remove(target);
        }

        [UnityTest]
        public IEnumerator Go_Find_ByName_FindsMatchingObjects()
        {
            CreateTestObject("PlayMode_GoFind_Match1");
            CreateTestObject("PlayMode_GoFind_Match2");
            CreateTestObject("PlayMode_GoFind_Other");

            yield return null;

            stdout = new StringBuilderTextWriter();
            // Note: find uses partial match (IndexOf), not glob pattern
            var task = terminal.ExecuteAsync("go find -n PlayMode_GoFind_Match", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_GoFind_Match1"), $"Match1 not found in: {output}");
            Assert.IsTrue(output.Contains("PlayMode_GoFind_Match2"), $"Match2 not found in: {output}");
        }

        [UnityTest]
        public IEnumerator Go_Find_ByComponent_FindsObjectsWithComponent()
        {
            var withRb = CreateTestObject("PlayMode_GoFindRb");
            withRb.AddComponent<Rigidbody>();
            CreateTestObject("PlayMode_GoFindNoRb");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("go find -c Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_GoFindRb"));
        }

        [UnityTest]
        public IEnumerator Go_Info_ShowsObjectInfo()
        {
            var target = CreateTestObject("PlayMode_GoInfo");
            target.AddComponent<BoxCollider>();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("go info /PlayMode_GoInfo", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_GoInfo"));
        }

        [UnityTest]
        public IEnumerator Go_Rename_ChangesObjectName()
        {
            var target = CreateTestObject("PlayMode_GoOldName");

            yield return null;

            // rename expects positional arguments: go rename <path> <new-name>
            var task = terminal.ExecuteAsync("go rename /PlayMode_GoOldName PlayMode_GoNewName", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual("PlayMode_GoNewName", target.name);
        }

        [UnityTest]
        public IEnumerator Go_Active_SetsActiveState()
        {
            var target = CreateTestObject("PlayMode_GoActive");
            Assert.IsTrue(target.activeSelf);

            yield return null;

            var task = terminal.ExecuteAsync("go active /PlayMode_GoActive -s false", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsFalse(target.activeSelf);
        }

        [UnityTest]
        public IEnumerator Go_Active_Toggle_TogglesState()
        {
            var target = CreateTestObject("PlayMode_GoToggle");
            Assert.IsTrue(target.activeSelf);

            yield return null;

            var task = terminal.ExecuteAsync("go active /PlayMode_GoToggle --toggle", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsFalse(target.activeSelf);

            stdout = new StringBuilderTextWriter();
            task = terminal.ExecuteAsync("go active /PlayMode_GoToggle --toggle", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.IsTrue(target.activeSelf);
        }

        [UnityTest]
        public IEnumerator Go_Clone_ClonesObject()
        {
            var original = CreateTestObject("PlayMode_GoOriginal");
            original.AddComponent<BoxCollider>();

            yield return null;

            var task = terminal.ExecuteAsync("go clone /PlayMode_GoOriginal -n PlayMode_GoCloned", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);

            yield return null;

            var cloned = GameObject.Find("PlayMode_GoCloned");
            Assert.IsNotNull(cloned);
            Assert.IsNotNull(cloned.GetComponent<BoxCollider>());
        }
    }
}
