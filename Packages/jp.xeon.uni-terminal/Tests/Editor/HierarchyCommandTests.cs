using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// hierarchyコマンドのテスト。
    /// </summary>
    public class HierarchyCommandTests
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
            // テストで作成したGameObjectを削除
            foreach (var go in createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            createdObjects.Clear();
        }

        /// <summary>
        /// テスト用GameObjectを作成します。
        /// </summary>
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

        // HIER-001 ルート一覧
        [Test]
        public async Task Hierarchy_Default_ShowsRootObjects()
        {
            var testObj = CreateTestObject("HierTest_Root1");
            var testObj2 = CreateTestObject("HierTest_Root2");

            var exitCode = await terminal.ExecuteAsync("hierarchy", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_Root1"));
            Assert.IsTrue(output.Contains("HierTest_Root2"));
        }

        // HIER-002 再帰表示
        [Test]
        public async Task Hierarchy_Recursive_ShowsAllChildren()
        {
            var root = CreateTestObject("HierTest_Parent");
            var child = CreateTestObject("HierTest_Child", root.transform);
            var grandChild = CreateTestObject("HierTest_GrandChild", child.transform);

            var exitCode = await terminal.ExecuteAsync("hierarchy -r", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_Parent"));
            Assert.IsTrue(output.Contains("HierTest_Child"));
            Assert.IsTrue(output.Contains("HierTest_GrandChild"));
        }

        // HIER-003 深度制限
        [Test]
        public async Task Hierarchy_Depth_LimitsDisplay()
        {
            var root = CreateTestObject("HierTest_Root");
            var level1 = CreateTestObject("HierTest_Level1", root.transform);
            var level2 = CreateTestObject("HierTest_Level2", level1.transform);
            var level3 = CreateTestObject("HierTest_Level3", level2.transform);

            // 深度1で制限（ルート + 直下の子まで表示）
            var exitCode = await terminal.ExecuteAsync("hierarchy -r -d 1", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_Root"));
            Assert.IsTrue(output.Contains("HierTest_Level1"));
            Assert.IsFalse(output.Contains("HierTest_Level2"));
            Assert.IsFalse(output.Contains("HierTest_Level3"));
        }

        // HIER-004 詳細表示
        [Test]
        public async Task Hierarchy_Long_ShowsDetailedInfo()
        {
            var testObj = CreateTestObject("HierTest_Detailed");
            testObj.AddComponent<BoxCollider>();

            var exitCode = await terminal.ExecuteAsync("hierarchy -l", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_Detailed"));
            Assert.IsTrue(output.Contains("[A]"));  // Active marker
            Assert.IsTrue(output.Contains("components"));
        }

        // HIER-010 パス指定
        [Test]
        public async Task Hierarchy_Path_ShowsSpecificObject()
        {
            var root = CreateTestObject("HierTest_PathRoot");
            var child1 = CreateTestObject("HierTest_PathChild1", root.transform);
            var child2 = CreateTestObject("HierTest_PathChild2", root.transform);

            var exitCode = await terminal.ExecuteAsync("hierarchy /HierTest_PathRoot", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_PathRoot"));
            Assert.IsTrue(output.Contains("HierTest_PathChild1"));
            Assert.IsTrue(output.Contains("HierTest_PathChild2"));
        }

        // HIER-011 深いパス指定
        [Test]
        public async Task Hierarchy_DeepPath_ShowsNestedObject()
        {
            var root = CreateTestObject("HierTest_DeepRoot");
            var middle = CreateTestObject("HierTest_Middle", root.transform);
            var leaf1 = CreateTestObject("HierTest_Leaf1", middle.transform);
            var leaf2 = CreateTestObject("HierTest_Leaf2", middle.transform);

            var exitCode = await terminal.ExecuteAsync("hierarchy /HierTest_DeepRoot/HierTest_Middle", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_Leaf1"));
            Assert.IsTrue(output.Contains("HierTest_Leaf2"));
        }

        // HIER-013 存在しないパス
        [Test]
        public async Task Hierarchy_InvalidPath_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("hierarchy /NonExistentObject12345", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // HIER-020 非アクティブを含む
        [Test]
        public async Task Hierarchy_All_ShowsInactiveObjects()
        {
            var active = CreateTestObject("HierTest_Active");
            var inactive = CreateTestObject("HierTest_Inactive");
            inactive.SetActive(false);

            // デフォルトでは非アクティブは表示されない
            stdout = new StringBuilderTextWriter();
            await terminal.ExecuteAsync("hierarchy", stdout, stderr);
            var outputWithoutAll = stdout.ToString();

            // -a オプションで非アクティブも表示
            stdout = new StringBuilderTextWriter();
            await terminal.ExecuteAsync("hierarchy -a", stdout, stderr);
            var outputWithAll = stdout.ToString();

            Assert.IsTrue(outputWithoutAll.Contains("HierTest_Active"));
            Assert.IsFalse(outputWithoutAll.Contains("HierTest_Inactive"));
            Assert.IsTrue(outputWithAll.Contains("HierTest_Active"));
            Assert.IsTrue(outputWithAll.Contains("HierTest_Inactive"));
        }

        // HIER-030 シーン一覧
        [Test]
        public async Task Hierarchy_SceneList_ShowsLoadedScenes()
        {
            var exitCode = await terminal.ExecuteAsync("hierarchy -s list", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Loaded Scenes:"));
        }

        // HIER-040 パイプ連携
        [Test]
        public async Task Hierarchy_Pipe_WorksWithGrep()
        {
            var testObj1 = CreateTestObject("HierTest_PipeTarget");
            var testObj2 = CreateTestObject("HierTest_Other");

            var exitCode = await terminal.ExecuteAsync("hierarchy | grep --pattern=PipeTarget", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("HierTest_PipeTarget"));
            Assert.IsFalse(output.Contains("HierTest_Other"));
        }

        // ツリー構造の表示テスト
        [Test]
        public async Task Hierarchy_TreeFormat_ShowsCorrectStructure()
        {
            var root = CreateTestObject("HierTest_TreeRoot");
            var child1 = CreateTestObject("HierTest_TreeChild1", root.transform);
            var child2 = CreateTestObject("HierTest_TreeChild2", root.transform);

            var exitCode = await terminal.ExecuteAsync("hierarchy /HierTest_TreeRoot", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            // ツリー記号が含まれていることを確認
            Assert.IsTrue(output.Contains("├") || output.Contains("└"));
        }
    }

    /// <summary>
    /// GameObjectPathユーティリティのテスト。
    /// </summary>
    public class GameObjectPathTests
    {
        private List<GameObject> createdObjects;

        [SetUp]
        public void SetUp()
        {
            createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
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

        [Test]
        public void Resolve_ValidPath_ReturnsGameObject()
        {
            var root = CreateTestObject("PathTest_Root");
            var child = CreateTestObject("PathTest_Child", root.transform);

            var result = GameObjectPath.Resolve("/PathTest_Root/PathTest_Child");

            Assert.IsNotNull(result);
            Assert.AreEqual("PathTest_Child", result.name);
        }

        [Test]
        public void Resolve_InvalidPath_ReturnsNull()
        {
            var result = GameObjectPath.Resolve("/NonExistent12345");
            Assert.IsNull(result);
        }

        [Test]
        public void Resolve_RootPath_ReturnsNull()
        {
            var result = GameObjectPath.Resolve("/");
            Assert.IsNull(result);
        }

        [Test]
        public void GetPath_ValidGameObject_ReturnsCorrectPath()
        {
            var root = CreateTestObject("PathTest_GetRoot");
            var child = CreateTestObject("PathTest_GetChild", root.transform);

            var path = GameObjectPath.GetPath(child);

            Assert.AreEqual("/PathTest_GetRoot/PathTest_GetChild", path);
        }

        [Test]
        public void GetPath_Null_ReturnsNull()
        {
            var path = GameObjectPath.GetPath(null);
            Assert.IsNull(path);
        }

        [Test]
        public void Exists_ValidPath_ReturnsTrue()
        {
            var obj = CreateTestObject("PathTest_Exists");

            Assert.IsTrue(GameObjectPath.Exists("/PathTest_Exists"));
        }

        [Test]
        public void Exists_InvalidPath_ReturnsFalse()
        {
            Assert.IsFalse(GameObjectPath.Exists("/NonExistent12345"));
        }

        [Test]
        public void GetCompletions_EmptyPrefix_ReturnsRootObjects()
        {
            var obj = CreateTestObject("PathTest_Complete");

            var completions = new List<string>(GameObjectPath.GetCompletions("/"));

            Assert.IsTrue(completions.Exists(c => c.Contains("PathTest_Complete")));
        }
    }
}
