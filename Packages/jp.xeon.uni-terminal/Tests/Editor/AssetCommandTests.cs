using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// assetコマンドのテスト。
    /// </summary>
    public class AssetCommandTests
    {
        private Terminal terminal;
        private StringBuilderTextWriter stdout;
        private StringBuilderTextWriter stderr;
        private List<UnityEngine.Object> createdAssets;

        [SetUp]
        public void SetUp()
        {
            terminal = new Terminal("/", "/", registerBuiltInCommands: true);
            stdout = new StringBuilderTextWriter();
            stderr = new StringBuilderTextWriter();
            createdAssets = new List<UnityEngine.Object>();
            AssetManager.ResetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in createdAssets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
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

        // ASSET-001 サブコマンドなし
        [Test]
        public async Task Asset_NoSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("missing subcommand"));
        }

        // ASSET-002 不明なサブコマンド
        [Test]
        public async Task Asset_UnknownSubcommand_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset unknowncmd", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("unknown subcommand"));
        }

        // ASSET-010 一覧表示（空）
        [Test]
        public async Task Asset_List_Empty_ShowsNoAssets()
        {
            var exitCode = await terminal.ExecuteAsync("asset list", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.IsTrue(stdout.ToString().Contains("No loaded assets"));
        }

        // ASSET-011 一覧表示（アセットあり）
        [Test]
        public async Task Asset_List_WithAssets_ShowsAssets()
        {
            var material = CreateMaterial("TestMaterial");
            AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset list", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("TestMaterial"));
            Assert.IsTrue(output.Contains($"#{material.GetInstanceID()}"));
        }

        // ASSET-012 一覧表示（型フィルター）
        [Test]
        public async Task Asset_List_WithTypeFilter_ShowsFilteredAssets()
        {
            var material = CreateMaterial("TestMaterial");
            var texture = CreateTexture("TestTexture");
            AssetManager.Instance.Registry.Register(material, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(texture, "path2", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset list -t Material", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("TestMaterial"));
            Assert.IsFalse(output.Contains("TestTexture"));
        }

        // ASSET-013 一覧表示（名前フィルター）
        [Test]
        public async Task Asset_List_WithNameFilter_ShowsFilteredAssets()
        {
            var material1 = CreateMaterial("RedMaterial");
            var material2 = CreateMaterial("BlueMaterial");
            AssetManager.Instance.Registry.Register(material1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(material2, "path2", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset list -n Red*", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("RedMaterial"));
            Assert.IsFalse(output.Contains("BlueMaterial"));
        }

        // ASSET-014 一覧表示（詳細形式）
        [Test]
        public async Task Asset_List_LongFormat_ShowsDetails()
        {
            var material = CreateMaterial("TestMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset list -l", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("TestMaterial"));
            Assert.IsTrue(output.Contains("Material"));
            Assert.IsTrue(output.Contains("TestProvider"));
        }

        // ASSET-020 情報表示
        [Test]
        public async Task Asset_Info_ShowsAssetDetails()
        {
            var material = CreateMaterial("InfoMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset info InfoMaterial", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("InfoMaterial"));
            Assert.IsTrue(output.Contains("Instance ID"));
            Assert.IsTrue(output.Contains("Type"));
            Assert.IsTrue(output.Contains("Provider"));
        }

        // ASSET-021 情報表示（インスタンスID指定）
        [Test]
        public async Task Asset_Info_ByInstanceId_ShowsAssetDetails()
        {
            var material = CreateMaterial("IdInfoMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var specifier = $"#{material.GetInstanceID()}";
            var exitCode = await terminal.ExecuteAsync($"asset info {specifier}", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("IdInfoMaterial"));
        }

        // ASSET-022 情報表示（存在しないアセット）
        [Test]
        public async Task Asset_Info_NotFound_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset info NonExistent12345", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // ASSET-023 情報表示（引数なし）
        [Test]
        public async Task Asset_Info_NoArgs_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset info", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("usage"));
        }

        // ASSET-024 情報表示（同名複数）
        [Test]
        public async Task Asset_Info_MultipleSameName_ReturnsError()
        {
            var material1 = CreateMaterial("SameName");
            var material2 = CreateMaterial("SameName");
            AssetManager.Instance.Registry.Register(material1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(material2, "path2", "TestProvider");

            var exitCode = await terminal.ExecuteAsync("asset info SameName", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            var errOutput = stderr.ToString();
            Assert.IsTrue(errOutput.Contains("multiple") || errOutput.Contains("matches"));
        }

        // ASSET-030 アンロード
        [Test]
        public async Task Asset_Unload_RemovesAsset()
        {
            var material = CreateMaterial("UnloadMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            Assert.AreEqual(1, AssetManager.Instance.Registry.Count);

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset unload UnloadMaterial", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(0, AssetManager.Instance.Registry.Count);
            Assert.IsTrue(stdout.ToString().Contains("Unloaded"));
        }

        // ASSET-031 アンロード（インスタンスID指定）
        [Test]
        public async Task Asset_Unload_ByInstanceId_RemovesAsset()
        {
            var material = CreateMaterial("IdUnloadMaterial");
            AssetManager.Instance.Registry.Register(material, "test/key", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var specifier = $"#{material.GetInstanceID()}";
            var exitCode = await terminal.ExecuteAsync($"asset unload {specifier}", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            Assert.AreEqual(0, AssetManager.Instance.Registry.Count);
        }

        // ASSET-032 アンロード（存在しないアセット）
        [Test]
        public async Task Asset_Unload_NotFound_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset unload NonExistent12345", stdout, stderr);

            Assert.AreEqual(ExitCode.RuntimeError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("not found"));
        }

        // ASSET-033 アンロード（引数なし）
        [Test]
        public async Task Asset_Unload_NoArgs_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset unload", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("usage"));
        }

        // ASSET-040 プロバイダー一覧（空）
        [Test]
        public async Task Asset_Providers_Empty_ShowsNoProviders()
        {
            var exitCode = await terminal.ExecuteAsync("asset providers", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            // 空の場合またはプロバイダーがある場合、どちらもSuccess
        }

        // ASSET-050 不明な型フィルター
        [Test]
        public async Task Asset_List_UnknownType_ReturnsError()
        {
            var exitCode = await terminal.ExecuteAsync("asset list -t NonExistentType12345", stdout, stderr);

            Assert.AreEqual(ExitCode.UsageError, exitCode);
            Assert.IsTrue(stderr.ToString().Contains("unknown type"));
        }

        // ASSET-060 パイプ連携
        [Test]
        public async Task Asset_List_PipeToGrep_Works()
        {
            var material1 = CreateMaterial("PipeMaterial1");
            var material2 = CreateMaterial("OtherAsset");
            AssetManager.Instance.Registry.Register(material1, "path1", "TestProvider");
            AssetManager.Instance.Registry.Register(material2, "path2", "TestProvider");

            stdout = new StringBuilderTextWriter();
            var exitCode = await terminal.ExecuteAsync("asset list | grep --pattern=PipeMaterial", stdout, stderr);

            Assert.AreEqual(ExitCode.Success, exitCode);
            var output = stdout.ToString();
            Assert.IsTrue(output.Contains("PipeMaterial1"));
            Assert.IsFalse(output.Contains("OtherAsset"));
        }
    }
}
