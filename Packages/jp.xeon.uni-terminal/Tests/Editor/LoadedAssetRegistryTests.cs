using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// LoadedAssetRegistryのテスト。
    /// </summary>
    public class LoadedAssetRegistryTests
    {
        private LoadedAssetRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = new LoadedAssetRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            registry.Clear();
        }

        // REG-001 アセット登録
        [Test]
        public void Register_ValidAsset_ReturnsEntry()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            var entry = registry.Register(material, "test/path", "TestProvider");

            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);
            Assert.AreEqual(material.GetInstanceID(), entry.InstanceId);
            Assert.AreEqual("TestMaterial", entry.Name);
            Assert.AreEqual("test/path", entry.Key);
            Assert.AreEqual("TestProvider", entry.ProviderName);
            Assert.AreEqual(typeof(Material), entry.AssetType);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-002 null登録
        [Test]
        public void Register_NullAsset_ReturnsNull()
        {
            var entry = registry.Register(null, "test/path", "TestProvider");
            Assert.IsNull(entry);
        }

        // REG-003 重複登録
        [Test]
        public void Register_DuplicateAsset_ReturnsExistingEntry()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            var entry1 = registry.Register(material, "path1", "Provider1");
            var entry2 = registry.Register(material, "path2", "Provider2");

            Assert.AreSame(entry1, entry2);
            Assert.AreEqual(1, registry.Count);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-010 インスタンスIDで取得
        [Test]
        public void GetByInstanceId_ExistingAsset_ReturnsEntry()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "test/path", "TestProvider");
            var entry = registry.GetByInstanceId(material.GetInstanceID());

            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-011 存在しないインスタンスID
        [Test]
        public void GetByInstanceId_NonExistingId_ReturnsNull()
        {
            var entry = registry.GetByInstanceId(999999);
            Assert.IsNull(entry);
        }

        // REG-020 名前で取得（一意）
        [Test]
        public void TryGetByName_UniqueName_ReturnsTrue()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "UniqueMaterial";

            registry.Register(material, "test/path", "TestProvider");
            var result = registry.TryGetByName("UniqueMaterial", out var entry);

            Assert.IsTrue(result);
            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-021 名前で取得（重複）
        [Test]
        public void TryGetByName_DuplicateName_ReturnsFalse()
        {
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "SameName";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "SameName";

            registry.Register(material1, "path1", "TestProvider");
            registry.Register(material2, "path2", "TestProvider");

            var result = registry.TryGetByName("SameName", out var entry);

            Assert.IsFalse(result);
            Assert.IsNull(entry);

            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
        }

        // REG-022 存在しない名前
        [Test]
        public void TryGetByName_NonExistingName_ReturnsFalse()
        {
            var result = registry.TryGetByName("NonExistent", out var entry);
            Assert.IsFalse(result);
            Assert.IsNull(entry);
        }

        // REG-023 名前で複数取得
        [Test]
        public void GetByName_DuplicateName_ReturnsAll()
        {
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "SameName";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "SameName";

            registry.Register(material1, "path1", "TestProvider");
            registry.Register(material2, "path2", "TestProvider");

            var entries = registry.GetByName("SameName");

            Assert.AreEqual(2, entries.Count);

            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
        }

        // REG-030 指定子解決（インスタンスID形式）
        [Test]
        public void TryResolve_InstanceIdFormat_ReturnsTrue()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "test/path", "TestProvider");
            var specifier = $"#{material.GetInstanceID()}";
            var result = registry.TryResolve(specifier, out var entry);

            Assert.IsTrue(result);
            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-031 指定子解決（名前）
        [Test]
        public void TryResolve_ByName_ReturnsTrue()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "ResolveByName";

            registry.Register(material, "test/path", "TestProvider");
            var result = registry.TryResolve("ResolveByName", out var entry);

            Assert.IsTrue(result);
            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-032 指定子解決（キー）
        [Test]
        public void TryResolve_ByKey_ReturnsTrue()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "unique/key/path", "TestProvider");
            var result = registry.TryResolve("unique/key/path", out var entry);

            Assert.IsTrue(result);
            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-033 空指定子
        [Test]
        public void TryResolve_EmptySpecifier_ReturnsFalse()
        {
            var result = registry.TryResolve("", out var entry);
            Assert.IsFalse(result);
            Assert.IsNull(entry);
        }

        // REG-034 null指定子
        [Test]
        public void TryResolve_NullSpecifier_ReturnsFalse()
        {
            var result = registry.TryResolve(null, out var entry);
            Assert.IsFalse(result);
            Assert.IsNull(entry);
        }

        // REG-040 アセット登録解除
        [Test]
        public void Unregister_ExistingAsset_ReturnsTrue()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "test/path", "TestProvider");
            var result = registry.Unregister(material);

            Assert.IsTrue(result);
            Assert.AreEqual(0, registry.Count);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-041 インスタンスIDで登録解除
        [Test]
        public void Unregister_ByInstanceId_ReturnsTrue()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "test/path", "TestProvider");
            var result = registry.Unregister(material.GetInstanceID());

            Assert.IsTrue(result);
            Assert.AreEqual(0, registry.Count);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-042 存在しないアセットの登録解除
        [Test]
        public void Unregister_NonExistingAsset_ReturnsFalse()
        {
            var result = registry.Unregister(999999);
            Assert.IsFalse(result);
        }

        // REG-050 全アセット取得
        [Test]
        public void GetAll_ReturnsAllRegisteredAssets()
        {
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "Material1";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "Material2";

            registry.Register(material1, "path1", "TestProvider");
            registry.Register(material2, "path2", "TestProvider");

            var entries = registry.GetAll().ToList();

            Assert.AreEqual(2, entries.Count);

            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
        }

        // REG-051 型フィルターで取得
        [Test]
        public void GetAll_WithTypeFilter_ReturnsFilteredAssets()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";
            var go = new GameObject("TestGO");

            registry.Register(material, "path1", "TestProvider");
            registry.Register(go, "path2", "TestProvider");

            var entries = registry.GetAll(typeof(Material)).ToList();

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(material, entries[0].Asset);

            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(go);
        }

        // REG-060 パターン検索
        [Test]
        public void Find_WithPattern_ReturnsMatchingAssets()
        {
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "RedMaterial";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "BlueMaterial";
            var material3 = new Material(Shader.Find("Standard"));
            material3.name = "GreenTexture";

            registry.Register(material1, "path1", "TestProvider");
            registry.Register(material2, "path2", "TestProvider");
            registry.Register(material3, "path3", "TestProvider");

            var entries = registry.Find("*Material").ToList();

            Assert.AreEqual(2, entries.Count);
            Assert.IsTrue(entries.Any(e => e.Name == "RedMaterial"));
            Assert.IsTrue(entries.Any(e => e.Name == "BlueMaterial"));

            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
            UnityEngine.Object.DestroyImmediate(material3);
        }

        // REG-061 ?ワイルドカードパターン
        [Test]
        public void Find_WithQuestionMarkPattern_ReturnsMatchingAssets()
        {
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "MatA";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "MatB";
            var material3 = new Material(Shader.Find("Standard"));
            material3.name = "MatAB";

            registry.Register(material1, "path1", "TestProvider");
            registry.Register(material2, "path2", "TestProvider");
            registry.Register(material3, "path3", "TestProvider");

            var entries = registry.Find("Mat?").ToList();

            Assert.AreEqual(2, entries.Count);
            Assert.IsTrue(entries.Any(e => e.Name == "MatA"));
            Assert.IsTrue(entries.Any(e => e.Name == "MatB"));

            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
            UnityEngine.Object.DestroyImmediate(material3);
        }

        // REG-062 キーでパターン検索
        [Test]
        public void Find_ByKeyPattern_ReturnsMatchingAssets()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "assets/materials/test", "TestProvider");

            var entries = registry.Find("assets/materials/*").ToList();

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(material, entries[0].Asset);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-070 クリア
        [Test]
        public void Clear_RemovesAllEntries()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            registry.Register(material, "test/path", "TestProvider");
            Assert.AreEqual(1, registry.Count);

            registry.Clear();

            Assert.AreEqual(0, registry.Count);

            UnityEngine.Object.DestroyImmediate(material);
        }

        // REG-080 登録解除後の名前インデックス更新
        [Test]
        public void Unregister_UpdatesNameIndex()
        {
            var material1 = new Material(Shader.Find("Standard"));
            material1.name = "SameName";
            var material2 = new Material(Shader.Find("Standard"));
            material2.name = "SameName";

            registry.Register(material1, "path1", "TestProvider");
            registry.Register(material2, "path2", "TestProvider");

            Assert.AreEqual(2, registry.GetByName("SameName").Count);

            registry.Unregister(material1);

            var entries = registry.GetByName("SameName");
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(material2, entries[0].Asset);

            // 今度は一意になったのでTryGetByNameが成功するはず
            var result = registry.TryGetByName("SameName", out var entry);
            Assert.IsTrue(result);
            Assert.AreEqual(material2, entry.Asset);

            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
        }
    }
}
