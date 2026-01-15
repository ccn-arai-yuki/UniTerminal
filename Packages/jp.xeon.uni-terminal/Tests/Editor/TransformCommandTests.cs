using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// transformコマンドのテスト。
    /// </summary>
    public class TransformCommandTests
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

        // TF-001 情報表示
        [Test]
        public async Task Transform_ShowsInfo()
        {
            var obj = CreateTestObject("TfTest_Info");
            obj.transform.position = new Vector3(1, 2, 3);

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Info", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("TfTest_Info"));
            Assert.IsTrue(output.Contains("World Position"));
            Assert.IsTrue(output.Contains("Local Position"));
        }

        // TF-002 存在しないパス
        [Test]
        public async Task Transform_NotFound_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("transform /TfTest_NonExistent", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // TF-010 ワールド位置設定
        [Test]
        public async Task Transform_SetPosition()
        {
            var obj = CreateTestObject("TfTest_Pos");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Pos --position 5,10,15", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(5, 10, 15), obj.transform.position);
        }

        // TF-011 ローカル位置設定
        [Test]
        public async Task Transform_SetLocalPosition()
        {
            var parent = CreateTestObject("TfTest_Parent");
            parent.transform.position = new Vector3(10, 0, 0);
            var child = CreateTestObject("TfTest_Child", parent.transform);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Parent/TfTest_Child --local-position 1,2,3", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(1, 2, 3), child.transform.localPosition);
        }

        // TF-012 単一値位置
        [Test]
        public async Task Transform_SetPosition_SingleValue()
        {
            var obj = CreateTestObject("TfTest_SinglePos");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_SinglePos --position 5", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(5, 5, 5), obj.transform.position);
        }

        // TF-020 回転設定
        [Test]
        public async Task Transform_SetRotation()
        {
            var obj = CreateTestObject("TfTest_Rot");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Rot --rotation 0,90,0", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            // 浮動小数点の誤差を考慮
            Assert.AreEqual(90f, obj.transform.eulerAngles.y, 0.1f);
        }

        // TF-021 ローカル回転設定
        [Test]
        public async Task Transform_SetLocalRotation()
        {
            var obj = CreateTestObject("TfTest_LocalRot");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_LocalRot --local-rotation 45,0,0", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(45f, obj.transform.localEulerAngles.x, 0.1f);
        }

        // TF-030 スケール設定
        [Test]
        public async Task Transform_SetScale()
        {
            var obj = CreateTestObject("TfTest_Scale");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Scale --scale 2,2,2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(2, 2, 2), obj.transform.localScale);
        }

        // TF-031 非均等スケール
        [Test]
        public async Task Transform_SetScale_NonUniform()
        {
            var obj = CreateTestObject("TfTest_NonUniformScale");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_NonUniformScale --scale 1,2,3", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(1, 2, 3), obj.transform.localScale);
        }

        // TF-040 親変更
        [Test]
        public async Task Transform_SetParent()
        {
            var parent = CreateTestObject("TfTest_NewParent");
            var obj = CreateTestObject("TfTest_Reparent");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Reparent --parent /TfTest_NewParent", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(parent.transform, obj.transform.parent);
        }

        // TF-041 親解除
        [Test]
        public async Task Transform_Unparent()
        {
            var parent = CreateTestObject("TfTest_OldParent");
            var child = CreateTestObject("TfTest_Unparent", parent.transform);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("transform /TfTest_OldParent/TfTest_Unparent --parent /", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsNull(child.transform.parent);
        }

        // TF-042 循環参照エラー
        [Test]
        public async Task Transform_CyclicParent_ReturnsError()
        {
            var parent = CreateTestObject("TfTest_CyclicParent");
            var child = CreateTestObject("TfTest_CyclicChild", parent.transform);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("transform /TfTest_CyclicParent --parent /TfTest_CyclicParent/TfTest_CyclicChild", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("cannot set parent"));
        }

        // TF-050 複合設定
        [Test]
        public async Task Transform_MultipleOptions()
        {
            var obj = CreateTestObject("TfTest_Multi");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_Multi --position 1,2,3 --rotation 0,90,0 --scale 2,2,2", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(new Vector3(1, 2, 3), obj.transform.position);
            Assert.AreEqual(90f, obj.transform.eulerAngles.y, 0.1f);
            Assert.AreEqual(new Vector3(2, 2, 2), obj.transform.localScale);
        }

        // パス引数なし
        [Test]
        public async Task Transform_NoPath_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("transform", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("missing path"));
        }

        // 無効なVector3
        [Test]
        public async Task Transform_InvalidVector_ReturnsError()
        {
            var obj = CreateTestObject("TfTest_InvalidVec");

            var exitCode = await terminal.ExecuteAsync("transform /TfTest_InvalidVec --position invalid", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("invalid"));
        }
    }
}
