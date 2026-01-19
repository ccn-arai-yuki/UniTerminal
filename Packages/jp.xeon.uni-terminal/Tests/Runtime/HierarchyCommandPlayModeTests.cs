using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// hierarchyコマンドのPlayModeテスト。
    /// </summary>
    public class HierarchyCommandPlayModeTests
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
        public IEnumerator Hierarchy_Default_ShowsRootObjects()
        {
            var testObj = CreateTestObject("PlayMode_HierRoot1");
            var testObj2 = CreateTestObject("PlayMode_HierRoot2");

            yield return null;

            var task = terminal.ExecuteAsync("hierarchy", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_HierRoot1"));
            Assert.IsTrue(output.Contains("PlayMode_HierRoot2"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_Recursive_ShowsAllChildren()
        {
            var root = CreateTestObject("PlayMode_Parent");
            var child = CreateTestObject("PlayMode_Child", root.transform);
            var grandChild = CreateTestObject("PlayMode_GrandChild", child.transform);

            yield return null;

            var task = terminal.ExecuteAsync("hierarchy -r", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_Parent"));
            Assert.IsTrue(output.Contains("PlayMode_Child"));
            Assert.IsTrue(output.Contains("PlayMode_GrandChild"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_LongFormat_ShowsDetails()
        {
            var testObj = CreateTestObject("PlayMode_Detailed");
            testObj.AddComponent<BoxCollider>();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -l", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_Detailed"));
            Assert.IsTrue(output.Contains("[A]") || output.Contains("[-]"));
            Assert.IsTrue(output.Contains("component"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_FilterByName_ShowsMatchingObjects()
        {
            CreateTestObject("PlayMode_FilterTest_Match");
            CreateTestObject("PlayMode_FilterTest_NoMatch");
            CreateTestObject("PlayMode_Other");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -n \"PlayMode_FilterTest*\"", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_FilterTest_Match"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_FilterByComponent_ShowsMatchingObjects()
        {
            var withRigidbody = CreateTestObject("PlayMode_WithRigidbody");
            withRigidbody.AddComponent<Rigidbody>();
            CreateTestObject("PlayMode_WithoutRigidbody");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -c Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_WithRigidbody"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_FilterByTag_ShowsMatchingObjects()
        {
            var tagged = CreateTestObject("PlayMode_Tagged");
            tagged.tag = "MainCamera";
            CreateTestObject("PlayMode_Untagged");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -t MainCamera", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_Tagged") || output.Contains("MainCamera"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_Path_ShowsSpecificObject()
        {
            var root = CreateTestObject("PlayMode_PathRoot");
            var child1 = CreateTestObject("PlayMode_Child1", root.transform);
            var child2 = CreateTestObject("PlayMode_Child2", root.transform);

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy /PlayMode_PathRoot", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_Child1"));
            Assert.IsTrue(output.Contains("PlayMode_Child2"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_IncludeInactive_ShowsInactiveObjects()
        {
            var active = CreateTestObject("PlayMode_Active");
            var inactive = CreateTestObject("PlayMode_Inactive");
            inactive.SetActive(false);

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -a", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_Active"));
            Assert.IsTrue(output.Contains("PlayMode_Inactive"));
        }

        // --- インスタンスID表示テスト ---

        [UnityTest]
        public IEnumerator Hierarchy_ShowInstanceId_DisplaysIds()
        {
            var testObj = CreateTestObject("PlayMode_InstanceId");
            var instanceId = testObj.GetInstanceID();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -i", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_InstanceId"));
            Assert.IsTrue(output.Contains($"#{instanceId}"), $"Output should contain #{instanceId}. Output: {output}");
        }

        [UnityTest]
        public IEnumerator Hierarchy_ShowInstanceIdWithLong_DisplaysBoth()
        {
            var testObj = CreateTestObject("PlayMode_IdLong");
            testObj.AddComponent<BoxCollider>();
            var instanceId = testObj.GetInstanceID();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -i -l", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_IdLong"));
            Assert.IsTrue(output.Contains($"#{instanceId}"));
            Assert.IsTrue(output.Contains("[A]") || output.Contains("[-]"));
        }

        [UnityTest]
        public IEnumerator Hierarchy_ShowInstanceIdRecursive_DisplaysAllIds()
        {
            var parent = CreateTestObject("PlayMode_IdParent");
            var child = CreateTestObject("PlayMode_IdChild", parent.transform);
            var parentId = parent.GetInstanceID();
            var childId = child.GetInstanceID();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy -r -i", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains($"#{parentId}"), $"Output should contain #{parentId}");
            Assert.IsTrue(output.Contains($"#{childId}"), $"Output should contain #{childId}");
        }

        [UnityTest]
        public IEnumerator Hierarchy_Default_DoesNotShowInstanceId()
        {
            var testObj = CreateTestObject("PlayMode_NoId");
            var instanceId = testObj.GetInstanceID();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("hierarchy", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_NoId"));
            Assert.IsFalse(output.Contains($"#{instanceId}"), $"Output should NOT contain #{instanceId} by default");
        }
    }
}
