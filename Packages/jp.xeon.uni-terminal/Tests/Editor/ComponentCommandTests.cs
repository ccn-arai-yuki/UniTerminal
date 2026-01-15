using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// componentコマンドのテスト。
    /// </summary>
    public class ComponentCommandTests
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

        // COMP-001 コンポーネント一覧表示
        [Test]
        public async Task Component_List_ShowsComponents()
        {
            var obj = CreateTestObject("CompTest_List");
            obj.AddComponent<BoxCollider>();
            obj.AddComponent<Rigidbody>();

            var exitCode = await terminal.ExecuteAsync("component list /CompTest_List", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Transform"));
            Assert.IsTrue(output.Contains("BoxCollider"));
            Assert.IsTrue(output.Contains("Rigidbody"));
        }

        // COMP-002 詳細表示
        [Test]
        public async Task Component_List_Verbose()
        {
            var obj = CreateTestObject("CompTest_Verbose");
            obj.AddComponent<BoxCollider>();

            var exitCode = await terminal.ExecuteAsync("component list /CompTest_Verbose -v", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("UnityEngine.Transform"));
            Assert.IsTrue(output.Contains("UnityEngine.BoxCollider"));
        }

        // COMP-003 存在しないパス
        [Test]
        public async Task Component_List_NotFound()
        {
            var exitCode = await terminal.ExecuteAsync("component list /CompTest_NonExistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // COMP-010 コンポーネント追加（Unity標準）
        [Test]
        public async Task Component_Add_UnityStandard()
        {
            var obj = CreateTestObject("CompTest_Add");

            var exitCode = await terminal.ExecuteAsync("component add /CompTest_Add Rigidbody", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNotNull(obj.GetComponent<Rigidbody>());
            Assert.IsTrue(stdout.ToString().Contains("Added:"));
        }

        // COMP-011 コンポーネント追加（BoxCollider）
        [Test]
        public async Task Component_Add_BoxCollider()
        {
            var obj = CreateTestObject("CompTest_AddBox");

            var exitCode = await terminal.ExecuteAsync("component add /CompTest_AddBox BoxCollider", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNotNull(obj.GetComponent<BoxCollider>());
        }

        // COMP-013 存在しない型の追加
        [Test]
        public async Task Component_Add_TypeNotFound()
        {
            var obj = CreateTestObject("CompTest_AddNoType");

            var exitCode = await terminal.ExecuteAsync("component add /CompTest_AddNoType NonExistentComponent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // COMP-020 コンポーネント削除（型名指定）
        [Test]
        public async Task Component_Remove_ByTypeName()
        {
            var obj = CreateTestObject("CompTest_Remove");
            obj.AddComponent<Rigidbody>();
            Assert.IsNotNull(obj.GetComponent<Rigidbody>());

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("component remove /CompTest_Remove Rigidbody --immediate", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNull(obj.GetComponent<Rigidbody>());
            Assert.IsTrue(stdout.ToString().Contains("Removed:"));
        }

        // COMP-021 コンポーネント削除（インデックス指定）
        [Test]
        public async Task Component_Remove_ByIndex()
        {
            var obj = CreateTestObject("CompTest_RemoveIndex");
            obj.AddComponent<BoxCollider>();
            Assert.IsNotNull(obj.GetComponent<BoxCollider>());

            stdout = new StringBuilderTextWriter();
            // インデックス1はBoxCollider（0はTransform）
            var exitCode = await terminal.ExecuteAsync("component remove /CompTest_RemoveIndex 1 --immediate", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNull(obj.GetComponent<BoxCollider>());
        }

        // COMP-022 Transform削除試行
        [Test]
        public async Task Component_Remove_Transform_Fails()
        {
            var obj = CreateTestObject("CompTest_RemoveTransform");

            var exitCode = await terminal.ExecuteAsync("component remove /CompTest_RemoveTransform Transform", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("Cannot remove Transform"));
        }

        // COMP-023 全コンポーネント削除
        [Test]
        public async Task Component_Remove_All()
        {
            var obj = CreateTestObject("CompTest_RemoveAll");
            obj.AddComponent<BoxCollider>();
            obj.AddComponent<BoxCollider>();
            Assert.AreEqual(2, obj.GetComponents<BoxCollider>().Length);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("component remove /CompTest_RemoveAll BoxCollider --all --immediate", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(0, obj.GetComponents<BoxCollider>().Length);
        }

        // COMP-030 コンポーネント情報表示
        [Test]
        public async Task Component_Info_ShowsProperties()
        {
            var obj = CreateTestObject("CompTest_Info");
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 5f;

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("component info /CompTest_Info Rigidbody", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("Component: Rigidbody"));
            Assert.IsTrue(output.Contains("Properties:"));
        }

        // COMP-031 インデックス指定での情報表示
        [Test]
        public async Task Component_Info_ByIndex()
        {
            var obj = CreateTestObject("CompTest_InfoIndex");
            obj.AddComponent<BoxCollider>();

            var exitCode = await terminal.ExecuteAsync("component info /CompTest_InfoIndex 1", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("BoxCollider"));
        }

        // COMP-040 コンポーネント無効化
        [Test]
        public async Task Component_Disable()
        {
            var obj = CreateTestObject("CompTest_Disable");
            var collider = obj.AddComponent<BoxCollider>();
            Assert.IsTrue(collider.enabled);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("component disable /CompTest_Disable BoxCollider", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsFalse(collider.enabled);
            Assert.IsTrue(stdout.ToString().Contains("disabled"));
        }

        // COMP-041 コンポーネント有効化
        [Test]
        public async Task Component_Enable()
        {
            var obj = CreateTestObject("CompTest_Enable");
            var collider = obj.AddComponent<BoxCollider>();
            collider.enabled = false;
            Assert.IsFalse(collider.enabled);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("component enable /CompTest_Enable BoxCollider", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(collider.enabled);
            Assert.IsTrue(stdout.ToString().Contains("enabled"));
        }

        // COMP-042 非Behaviourコンポーネントのenable/disable
        [Test]
        public async Task Component_Enable_NonBehaviour_Fails()
        {
            var obj = CreateTestObject("CompTest_EnableNonBehaviour");
            // Transformは非Behaviourなので、別のコンポーネントを追加してテスト
            // ただし、ほとんどのコンポーネントはBehaviourかComponent
            // MeshFilterはBehaviourではないがenabledプロパティがない

            var exitCode = await terminal.ExecuteAsync("component disable /CompTest_EnableNonBehaviour Transform", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("does not support enable/disable"));
        }

        // サブコマンドなし
        [Test]
        public async Task Component_NoSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("component", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        // 不明なサブコマンド
        [Test]
        public async Task Component_UnknownSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("component unknowncmd /Test", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("unknown subcommand"));
        }

        // インデックス範囲外
        [Test]
        public async Task Component_Remove_IndexOutOfRange()
        {
            var obj = CreateTestObject("CompTest_IndexOutOfRange");

            var exitCode = await terminal.ExecuteAsync("component remove /CompTest_IndexOutOfRange 99", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("out of range"));
        }
    }
}
