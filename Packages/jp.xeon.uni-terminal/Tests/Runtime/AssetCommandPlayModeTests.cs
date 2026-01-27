using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.Tests.Runtime
{
    /// <summary>
    /// assetコマンドのPlayModeテスト
    /// </summary>
    public class AssetCommandPlayModeTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private List<Object> createdAssets;

        [SetUp]
        public void SetUp()
        {
            terminal = new Terminal("/", "/", registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
            createdAssets = new List<Object>();
            AssetManager.ResetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in createdAssets)
            {
                if (asset != null)
                    Object.Destroy(asset);
            }
            createdAssets.Clear();
            AssetManager.Instance.Registry.Clear();
            AssetManager.ResetInstance();
        }

        private Material CreateMaterial(string name)
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = name;
            createdAssets.Add(material);
            return material;
        }

        private Texture2D CreateTexture(string name)
        {
            var texture = new Texture2D(4, 4);
            texture.name = name;
            createdAssets.Add(texture);
            return texture;
        }

        private Mesh CreateMesh(string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
            createdAssets.Add(mesh);
            return mesh;
        }

        [UnityTest]
        public IEnumerator Asset_NoSubcommand_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.UsageError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        [UnityTest]
        public IEnumerator Asset_UnknownSubcommand_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset unknowncmd", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.UsageError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("unknown subcommand"));
        }

        [UnityTest]
        public IEnumerator Asset_List_Empty_ShowsNoAssets()
        {
            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.IsTrue(stdout.ToString().Contains("No loaded assets"));
        }

        [UnityTest]
        public IEnumerator Asset_List_WithAssets_ShowsAssets()
        {
            var material = CreateMaterial("PlayMode_AssetMaterial");
            AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_AssetMaterial"));
            Assert.IsTrue(output.Contains($"#{material.GetInstanceID()}"));
        }

        [UnityTest]
        public IEnumerator Asset_List_WithTypeFilter_ShowsFilteredAssets()
        {
            var material = CreateMaterial("PlayMode_FilterMaterial");
            var texture = CreateTexture("PlayMode_FilterTexture");
            AssetManager.Instance.Registry.Register(material, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(texture, "path2", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list -t Material", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_FilterMaterial"));
            Assert.IsFalse(output.Contains("PlayMode_FilterTexture"));
        }

        [UnityTest]
        public IEnumerator Asset_List_WithNameFilter_ShowsFilteredAssets()
        {
            var material1 = CreateMaterial("PlayMode_RedMaterial");
            var material2 = CreateMaterial("PlayMode_BlueMaterial");
            AssetManager.Instance.Registry.Register(material1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(material2, "path2", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list -n PlayMode_Red*", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_RedMaterial"));
            Assert.IsFalse(output.Contains("PlayMode_BlueMaterial"));
        }

        [UnityTest]
        public IEnumerator Asset_List_LongFormat_ShowsDetails()
        {
            var material = CreateMaterial("PlayMode_DetailMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list -l", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_DetailMaterial"));
            Assert.IsTrue(output.Contains("Material"));
            Assert.IsTrue(output.Contains("TestProvider"));
        }

        [UnityTest]
        public IEnumerator Asset_Info_ShowsAssetDetails()
        {
            var material = CreateMaterial("PlayMode_InfoMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset info PlayMode_InfoMaterial", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_InfoMaterial"));
            Assert.IsTrue(output.Contains("Instance ID"));
            Assert.IsTrue(output.Contains("Type"));
            Assert.IsTrue(output.Contains("Provider"));
        }

        [UnityTest]
        public IEnumerator Asset_Info_ByInstanceId_ShowsAssetDetails()
        {
            var material = CreateMaterial("PlayMode_IdInfoMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var specifier = $"#{material.GetInstanceID()}";
            var task = terminal.ExecuteAsync($"asset info {specifier}", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_IdInfoMaterial"));
        }

        [UnityTest]
        public IEnumerator Asset_Info_NotFound_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset info NonExistent12345", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        [UnityTest]
        public IEnumerator Asset_Unload_RemovesAsset()
        {
            var material = CreateMaterial("PlayMode_UnloadMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            Assert.AreEqual(1, AssetManager.Instance.Registry.Count);

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset unload PlayMode_UnloadMaterial", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(0, AssetManager.Instance.Registry.Count);
            Assert.IsTrue(stdout.ToString().Contains("Unloaded"));
        }

        [UnityTest]
        public IEnumerator Asset_Unload_ByInstanceId_RemovesAsset()
        {
            var material = CreateMaterial("PlayMode_IdUnloadMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var specifier = $"#{material.GetInstanceID()}";
            var task = terminal.ExecuteAsync($"asset unload {specifier}", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            Assert.AreEqual(0, AssetManager.Instance.Registry.Count);
        }

        [UnityTest]
        public IEnumerator Asset_Unload_NotFound_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset unload NonExistent12345", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.RuntimeError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        [UnityTest]
        public IEnumerator Asset_Providers_Success()
        {
            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset providers", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
        }

        [UnityTest]
        public IEnumerator Asset_List_UnknownType_ReturnsError()
        {
            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list -t NonExistentType12345", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.UsageError, task.Result);
            Assert.IsTrue(stderr.ToString().Contains("unknown type"));
        }

        [UnityTest]
        public IEnumerator Asset_List_MultipleAssetTypes_ShowsAll()
        {
            var material = CreateMaterial("PlayMode_MultiMaterial");
            var texture = CreateTexture("PlayMode_MultiTexture");
            var mesh = CreateMesh("PlayMode_MultiMesh");
            AssetManager.Instance.Registry.Register(material, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(texture, "path2", "TestProvider");
            AssetManager.Instance.Registry.Register(mesh, "path3", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_MultiMaterial"));
            Assert.IsTrue(output.Contains("PlayMode_MultiTexture"));
            Assert.IsTrue(output.Contains("PlayMode_MultiMesh"));
        }

        [UnityTest]
        public IEnumerator Asset_Info_MultipleSameName_ReturnsError()
        {
            var material1 = CreateMaterial("PlayMode_SameName");
            var material2 = CreateMaterial("PlayMode_SameName");
            AssetManager.Instance.Registry.Register(material1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(material2, "path2", "TestProvider");

            yield return null;

            stderr = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset info PlayMode_SameName", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.UsageError, task.Result);
            var errOutput = stderr.ToString();
            Assert.IsTrue(errOutput.Contains("multiple") || errOutput.Contains("matches"));
        }

        [UnityTest]
        public IEnumerator Asset_PipeToGrep_Works()
        {
            var material1 = CreateMaterial("PlayMode_PipeMaterial");
            var material2 = CreateMaterial("PlayMode_OtherAsset");
            AssetManager.Instance.Registry.Register(material1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(material2, "path2", "TestProvider");

            yield return null;

            stdout = new StringBuilderTextWriter();
            var task = terminal.ExecuteAsync("asset list | grep --pattern=PipeMaterial", stdout, stderr);
            while (!task.IsCompleted) yield return null;

            Assert.AreEqual(ExitCode.Success, task.Result);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PlayMode_PipeMaterial"));
            Assert.IsFalse(output.Contains("PlayMode_OtherAsset"));
        }
    }
}
