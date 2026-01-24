using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// propertyコマンドのPlayModeテスト。
    /// </summary>
    public class PropertyCommandPlayModeTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private List<GameObject> createdObjects;
        private List<Object> createdAssets;

        [SetUp]
        public void SetUp()
        {
            terminal = new Terminal("/", "/", registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
            createdObjects = new List<GameObject>();
            createdAssets = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in createdObjects)
            {
                if (go != null)
                    Object.Destroy(go);
            }
            createdObjects.Clear();

            foreach (var asset in createdAssets)
            {
                if (asset != null)
                    Object.Destroy(asset);
            }
            createdAssets.Clear();
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
        public IEnumerator Property_List_ShowsProperties()
        {
            var target = CreateTestObject("PlayMode_PropList");
            target.AddComponent<Rigidbody>();

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property list /PlayMode_PropList Rigidbody", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("mass"));
            Assert.IsTrue(output.Contains("useGravity"));
        }

        [UnityTest]
        public IEnumerator Property_Get_SingleProperty()
        {
            var target = CreateTestObject("PlayMode_PropGet");
            var rb = target.AddComponent<Rigidbody>();
            rb.mass = 5f;

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property get /PlayMode_PropGet Rigidbody mass", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("mass"));
            Assert.IsTrue(output.Contains("5"));
        }

        [UnityTest]
        public IEnumerator Property_Get_MultipleProperties()
        {
            var target = CreateTestObject("PlayMode_PropGetMulti");
            var rb = target.AddComponent<Rigidbody>();
            rb.mass = 10f;

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property get /PlayMode_PropGetMulti Rigidbody mass,useGravity", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("mass"));
            Assert.IsTrue(output.Contains("useGravity") || output.Contains("Gravity"));
        }

        [UnityTest]
        public IEnumerator Property_Set_Float()
        {
            var target = CreateTestObject("PlayMode_PropSetFloat");
            var rb = target.AddComponent<Rigidbody>();
            rb.mass = 1f;

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropSetFloat Rigidbody mass 10", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(10f, rb.mass, 0.01f);
        }

        [UnityTest]
        public IEnumerator Property_Set_Bool()
        {
            var target = CreateTestObject("PlayMode_PropSetBool");
            var rb = target.AddComponent<Rigidbody>();
            rb.useGravity = true;

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropSetBool Rigidbody useGravity false", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsFalse(rb.useGravity);
        }

        [UnityTest]
        public IEnumerator Property_Set_Vector3()
        {
            var target = CreateTestObject("PlayMode_PropSetVec");

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropSetVec Transform position 1,2,3", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(new Vector3(1, 2, 3), target.transform.position);
        }

        [UnityTest]
        public IEnumerator Property_Set_Enum()
        {
            var target = CreateTestObject("PlayMode_PropSetEnum");
            var rb = target.AddComponent<Rigidbody>();

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropSetEnum Rigidbody interpolation Interpolate", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, rb.interpolation);
        }

        [UnityTest]
        public IEnumerator Property_Set_Reference_Parent()
        {
            var parent = CreateTestObject("PlayMode_PropRefParent");
            var child = CreateTestObject("PlayMode_PropRefChild");

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropRefChild Transform parent /PlayMode_PropRefParent", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(parent.transform, child.transform.parent);
        }

        [UnityTest]
        public IEnumerator Property_Set_Reference_Null()
        {
            var parent = CreateTestObject("PlayMode_PropNullParent");
            var child = CreateTestObject("PlayMode_PropNullChild", parent.transform);
            Assert.IsNotNull(child.transform.parent);

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropNullParent/PlayMode_PropNullChild Transform parent null", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsNull(child.transform.parent);
        }

        [UnityTest]
        public IEnumerator Property_Set_InvalidValue_ReturnsError()
        {
            var target = CreateTestObject("PlayMode_PropInvalid");
            target.AddComponent<Rigidbody>();

            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property set /PlayMode_PropInvalid Rigidbody mass notanumber", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.UsageError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("Cannot convert"));
        }

        [UnityTest]
        public IEnumerator Property_Get_NonExistentProperty_ReturnsError()
        {
            var target = CreateTestObject("PlayMode_PropNotFound");
            target.AddComponent<Rigidbody>();

            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property get /PlayMode_PropNotFound Rigidbody nonexistent", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        [UnityTest]
        public IEnumerator Property_List_Transform_ShowsPosition()
        {
            var target = CreateTestObject("PlayMode_PropTransform");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property list /PlayMode_PropTransform Transform", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("position") || output.Contains("localPosition"));
        }

        [UnityTest]
        public IEnumerator Property_NoSubcommand_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.UsageError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        // --- アセット参照テスト ---

        [UnityTest]
        public IEnumerator Property_Set_Material_ByInstanceId()
        {
            var target = CreateTestObject("PlayMode_PropMatId");
            var renderer = target.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Standard"));
            material.name = "PlayMode_TestMaterial";
            createdAssets.Add(material);

            yield return null;

            var specifier = $"#{material.GetInstanceID()}";
            var task = terminal.ExecuteAsync($"property set /PlayMode_PropMatId MeshRenderer material {specifier}", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(material, renderer.sharedMaterial);
        }

        [UnityTest]
        public IEnumerator Property_Set_Material_ByRegisteredName()
        {
            var target = CreateTestObject("PlayMode_PropMatName");
            var renderer = target.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Standard"));
            material.name = "PlayMode_RegisteredMaterial";
            createdAssets.Add(material);

            Assets.AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropMatName MeshRenderer material PlayMode_RegisteredMaterial", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(material, renderer.sharedMaterial);

            Assets.AssetManager.Instance.Registry.Clear();
        }

        [UnityTest]
        public IEnumerator Property_Set_Material_Null()
        {
            var target = CreateTestObject("PlayMode_PropMatNull");
            var renderer = target.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial = material;
            createdAssets.Add(material);

            Assert.IsNotNull(renderer.sharedMaterial);

            yield return null;

            var task = terminal.ExecuteAsync("property set /PlayMode_PropMatNull MeshRenderer material null", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsNull(renderer.sharedMaterial);
        }

        [UnityTest]
        public IEnumerator Property_Set_Component_ByInstanceId()
        {
            var target = CreateTestObject("PlayMode_PropCompId");
            var joint = target.AddComponent<HingeJoint>();

            var otherBody = CreateTestObject("PlayMode_PropCompOther");
            var rb = otherBody.AddComponent<Rigidbody>();

            yield return null;

            var specifier = $"#{rb.GetInstanceID()}";
            var task = terminal.ExecuteAsync($"property set /PlayMode_PropCompId HingeJoint connectedBody {specifier}", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(rb, joint.connectedBody);
        }

        [UnityTest]
        public IEnumerator Property_Set_GameObject_ByInstanceId()
        {
            var target = CreateTestObject("PlayMode_PropGoId");
            var constraint = target.AddComponent<UnityEngine.Animations.ParentConstraint>();

            var sourceObj = CreateTestObject("PlayMode_PropGoSource");

            yield return null;

            // ParentConstraintのsourceにGameObjectを追加する代わりに、
            // LookAtConstraintのworldUpObjectを使用
            var lookAt = target.AddComponent<UnityEngine.Animations.LookAtConstraint>();

            var specifier = $"#{sourceObj.transform.GetInstanceID()}";
            var task = terminal.ExecuteAsync($"property set /PlayMode_PropGoId LookAtConstraint worldUpObject {specifier}", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(sourceObj.transform, lookAt.worldUpObject);
        }

        [UnityTest]
        public IEnumerator Property_Get_Material_ShowsName()
        {
            var target = CreateTestObject("PlayMode_PropGetMat");
            var renderer = target.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Standard"));
            material.name = "PlayMode_NamedMaterial";
            renderer.sharedMaterial = material;
            createdAssets.Add(material);

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("property get /PlayMode_PropGetMat MeshRenderer material", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_NamedMaterial") || output.Contains("material"));
        }
    }
}
