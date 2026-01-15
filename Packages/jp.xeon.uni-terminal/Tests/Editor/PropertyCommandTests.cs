using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// propertyコマンドのテスト。
    /// </summary>
    public class PropertyCommandTests
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

        // PROP-001 プロパティ一覧表示
        [Test]
        public async Task Property_List_ShowsProperties()
        {
            var obj = CreateTestObject("PropTest_List");
            obj.AddComponent<Rigidbody>();

            var exitCode = await terminal.ExecuteAsync("property list /PropTest_List Rigidbody", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("mass"));
            Assert.IsTrue(output.Contains("useGravity"));
        }

        // PROP-003 存在しないパス
        [Test]
        public async Task Property_List_NotFound()
        {
            var exitCode = await terminal.ExecuteAsync("property list /PropTest_NonExistent Transform", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // PROP-010 単一プロパティ取得
        [Test]
        public async Task Property_Get_Single()
        {
            var obj = CreateTestObject("PropTest_GetSingle");
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 5f;

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("property get /PropTest_GetSingle Rigidbody mass", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("mass"));
            Assert.IsTrue(output.Contains("5"));
        }

        // PROP-011 複数プロパティ取得
        [Test]
        public async Task Property_Get_Multiple()
        {
            var obj = CreateTestObject("PropTest_GetMultiple");
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.linearDamping = 2f;

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("property get /PropTest_GetMultiple Rigidbody mass,drag", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("mass"));
            Assert.IsTrue(output.Contains("drag"));
        }

        // PROP-012 存在しないプロパティ
        [Test]
        public async Task Property_Get_NotFound()
        {
            var obj = CreateTestObject("PropTest_GetNotFound");
            obj.AddComponent<Rigidbody>();

            var exitCode = await terminal.ExecuteAsync("property get /PropTest_GetNotFound Rigidbody nonexistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // PROP-020 float設定
        [Test]
        public async Task Property_Set_Float()
        {
            var obj = CreateTestObject("PropTest_SetFloat");
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 1f;

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("property set /PropTest_SetFloat Rigidbody mass 10", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(10f, rb.mass, 0.01f);
        }

        // PROP-021 bool設定
        [Test]
        public async Task Property_Set_Bool()
        {
            var obj = CreateTestObject("PropTest_SetBool");
            var rb = obj.AddComponent<Rigidbody>();
            rb.useGravity = true;

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("property set /PropTest_SetBool Rigidbody useGravity false", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsFalse(rb.useGravity);
        }

        // PROP-022 Vector3設定
        [Test]
        public async Task Property_Set_Vector3()
        {
            var obj = CreateTestObject("PropTest_SetVector3");

            var exitCode = await terminal.ExecuteAsync("property set /PropTest_SetVector3 Transform position 1,2,3", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(1, 2, 3), obj.transform.position);
        }

        // PROP-024 enum設定
        [Test]
        public async Task Property_Set_Enum()
        {
            var obj = CreateTestObject("PropTest_SetEnum");
            var rb = obj.AddComponent<Rigidbody>();

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("property set /PropTest_SetEnum Rigidbody interpolation Interpolate", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, rb.interpolation);
        }

        // PROP-026 型変換エラー
        [Test]
        public async Task Property_Set_TypeError()
        {
            var obj = CreateTestObject("PropTest_SetTypeError");
            obj.AddComponent<Rigidbody>();

            var exitCode = await terminal.ExecuteAsync("property set /PropTest_SetTypeError Rigidbody mass notanumber", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("Cannot convert"));
        }

        // サブコマンドなし
        [Test]
        public async Task Property_NoSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("property", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        // 不明なサブコマンド
        [Test]
        public async Task Property_UnknownSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("property unknowncmd /Test Transform", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("unknown subcommand"));
        }

        // コンポーネントが見つからない
        [Test]
        public async Task Property_ComponentNotFound()
        {
            var obj = CreateTestObject("PropTest_CompNotFound");

            var exitCode = await terminal.ExecuteAsync("property list /PropTest_CompNotFound Rigidbody", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }
    }
}
