using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// goコマンドのテスト
    /// </summary>
    public class GoCommandTests
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

        // GO-001 空オブジェクト作成
        [Test]
        public async Task Go_Create_EmptyObject()
        {
            var exitCode = await terminal.ExecuteAsync("go create GoTest_NewObj", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Created:"));
            Assert.IsTrue(output.Contains("GoTest_NewObj"));

            // クリーンアップ用に追加
            var created = GameObject.Find("GoTest_NewObj");
            if (created != null) createdObjects.Add(created);
        }

        // GO-002 プリミティブ作成
        [Test]
        public async Task Go_Create_Primitive()
        {
            var exitCode = await terminal.ExecuteAsync("go create GoTest_Cube --primitive Cube", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var created = GameObject.Find("GoTest_Cube");
            Assert.IsNotNull(created);
            Assert.IsNotNull(created.GetComponent<MeshFilter>());
            createdObjects.Add(created);
        }

        // GO-003 親指定で作成
        [Test]
        public async Task Go_Create_WithParent()
        {
            var parent = CreateTestObject("GoTest_Parent");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go create GoTest_Child --parent /GoTest_Parent", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var child = GameObject.Find("GoTest_Child");
            Assert.IsNotNull(child);
            Assert.AreEqual(parent.transform, child.transform.parent);
            createdObjects.Add(child);
        }

        // GO-004 位置指定
        [Test]
        public async Task Go_Create_WithPosition()
        {
            var exitCode = await terminal.ExecuteAsync("go create GoTest_Positioned --position 1,2,3", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var created = GameObject.Find("GoTest_Positioned");
            Assert.IsNotNull(created);
            Assert.AreEqual(new Vector3(1, 2, 3), created.transform.position);
            createdObjects.Add(created);
        }

        // GO-010 オブジェクト削除
        [Test]
        public async Task Go_Delete_Object()
        {
            var obj = CreateTestObject("GoTest_ToDelete");
            createdObjects.Remove(obj); // 削除テストなのでリストから除外

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go delete /GoTest_ToDelete --immediate", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNull(GameObject.Find("GoTest_ToDelete"));
        }

        // GO-011 子のみ削除
        [Test]
        public async Task Go_Delete_ChildrenOnly()
        {
            var parent = CreateTestObject("GoTest_ParentKeep");
            var child1 = CreateTestObject("GoTest_Child1", parent.transform);
            var child2 = CreateTestObject("GoTest_Child2", parent.transform);
            createdObjects.Remove(child1);
            createdObjects.Remove(child2);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go delete /GoTest_ParentKeep --children --immediate", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNotNull(GameObject.Find("GoTest_ParentKeep"));
            Assert.AreEqual(0, parent.transform.childCount);
        }

        // GO-012 存在しないオブジェクト削除
        [Test]
        public async Task Go_Delete_NotFound()
        {
            var exitCode = await terminal.ExecuteAsync("go delete /GoTest_NonExistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // GO-020 名前検索
        [Test]
        public async Task Go_Find_ByName()
        {
            var obj1 = CreateTestObject("GoTest_FindTarget1");
            var obj2 = CreateTestObject("GoTest_FindTarget2");
            var obj3 = CreateTestObject("GoTest_Other");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go find --name FindTarget", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("GoTest_FindTarget1"));
            Assert.IsTrue(output.Contains("GoTest_FindTarget2"));
            Assert.IsFalse(output.Contains("GoTest_Other"));
        }

        // GO-030 名前変更
        [Test]
        public async Task Go_Rename_Object()
        {
            var obj = CreateTestObject("GoTest_OldName");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go rename /GoTest_OldName GoTest_NewName", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual("GoTest_NewName", obj.name);
        }

        // GO-031 アクティブ状態切り替え
        [Test]
        public async Task Go_Active_Toggle()
        {
            var obj = CreateTestObject("GoTest_ActiveToggle");
            Assert.IsTrue(obj.activeSelf);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go active /GoTest_ActiveToggle --toggle", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsFalse(obj.activeSelf);

            stdout = new StringBuilderTextWriter();
            await terminal.ExecuteAsync("go active /GoTest_ActiveToggle --toggle", stdout, stderr);
            Assert.IsTrue(obj.activeSelf);
        }

        // GO-032 複製
        [Test]
        public async Task Go_Clone_Object()
        {
            var obj = CreateTestObject("GoTest_Original");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go clone /GoTest_Original --name GoTest_Cloned", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var cloned = GameObject.Find("GoTest_Cloned");
            Assert.IsNotNull(cloned);
            createdObjects.Add(cloned);
        }

        // GO-033 情報表示
        [Test]
        public async Task Go_Info_ShowsDetails()
        {
            var obj = CreateTestObject("GoTest_InfoTarget");
            obj.AddComponent<BoxCollider>();

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("go info /GoTest_InfoTarget", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("GoTest_InfoTarget"));
            Assert.IsTrue(output.Contains("Transform"));
            Assert.IsTrue(output.Contains("BoxCollider"));
        }

        // サブコマンドなし
        [Test]
        public async Task Go_NoSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("go", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        // 不明なサブコマンド
        [Test]
        public async Task Go_UnknownSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("go unknowncmd", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("unknown subcommand"));
        }
    }
}
