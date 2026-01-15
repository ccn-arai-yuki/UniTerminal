using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// transformコマンドのPlayModeテスト。
    /// </summary>
    public class TransformCommandPlayModeTests
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
        public IEnumerator Transform_Position_SetsWorldPosition()
        {
            var target = CreateTestObject("PlayMode_TransPos");

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransPos -p 1,2,3", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(new Vector3(1, 2, 3), target.transform.position);
        }

        [UnityTest]
        public IEnumerator Transform_LocalPosition_SetsLocalPosition()
        {
            var parent = CreateTestObject("PlayMode_TransParent");
            parent.transform.position = new Vector3(10, 10, 10);
            var child = CreateTestObject("PlayMode_TransChild", parent.transform);

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransParent/PlayMode_TransChild -P 1,1,1", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(new Vector3(1, 1, 1), child.transform.localPosition);
        }

        [UnityTest]
        public IEnumerator Transform_Rotation_SetsWorldRotation()
        {
            var target = CreateTestObject("PlayMode_TransRot");

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransRot -r 0,90,0", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(90f, target.transform.eulerAngles.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Transform_LocalRotation_SetsLocalRotation()
        {
            var parent = CreateTestObject("PlayMode_TransRotParent");
            parent.transform.rotation = Quaternion.Euler(0, 45, 0);
            var child = CreateTestObject("PlayMode_TransRotChild", parent.transform);

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransRotParent/PlayMode_TransRotChild -R 0,45,0", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(45f, child.transform.localEulerAngles.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Transform_Scale_SetsLocalScale()
        {
            var target = CreateTestObject("PlayMode_TransScale");

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransScale -s 2,3,4", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(new Vector3(2, 3, 4), target.transform.localScale);
        }

        [UnityTest]
        public IEnumerator Transform_Parent_SetsParent()
        {
            var parent = CreateTestObject("PlayMode_TransNewParent");
            var child = CreateTestObject("PlayMode_TransOrphan");

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransOrphan --parent /PlayMode_TransNewParent", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(parent.transform, child.transform.parent);
        }

        [UnityTest]
        public IEnumerator Transform_Parent_Null_UnparentsObject()
        {
            var parent = CreateTestObject("PlayMode_TransUnparentParent");
            var child = CreateTestObject("PlayMode_TransUnparentChild", parent.transform);

            Assert.IsNotNull(child.transform.parent);

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransUnparentParent/PlayMode_TransUnparentChild --parent null", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsNull(child.transform.parent);
        }

        [UnityTest]
        public IEnumerator Transform_Combined_SetsMultipleProperties()
        {
            var target = CreateTestObject("PlayMode_TransCombined");

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransCombined -p 5,5,5 -r 45,45,45 -s 2,2,2", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(new Vector3(5, 5, 5), target.transform.position);
            Assert.AreEqual(new Vector3(2, 2, 2), target.transform.localScale);
            Assert.AreEqual(45f, target.transform.eulerAngles.x, 0.1f);
        }

        [UnityTest]
        public IEnumerator Transform_NoOptions_ShowsCurrentTransform()
        {
            var target = CreateTestObject("PlayMode_TransInfo");
            target.transform.position = new Vector3(1, 2, 3);
            target.transform.localScale = new Vector3(2, 2, 2);

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("transform /PlayMode_TransInfo", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("position") || output.Contains("Position"));
        }

        [UnityTest]
        public IEnumerator Transform_WorldPositionStays_MaintainsWorldPosition()
        {
            var parent = CreateTestObject("PlayMode_TransWorldParent");
            parent.transform.position = new Vector3(10, 0, 0);
            var child = CreateTestObject("PlayMode_TransWorldChild");
            child.transform.position = new Vector3(5, 5, 5);

            yield return null;

            var task = terminal.ExecuteAsync("transform /PlayMode_TransWorldChild --parent /PlayMode_TransWorldParent -w", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(parent.transform, child.transform.parent);
            // ワールド座標が維持されている
            Assert.AreEqual(5f, child.transform.position.x, 0.1f);
            Assert.AreEqual(5f, child.transform.position.y, 0.1f);
            Assert.AreEqual(5f, child.transform.position.z, 0.1f);
        }
    }
}
