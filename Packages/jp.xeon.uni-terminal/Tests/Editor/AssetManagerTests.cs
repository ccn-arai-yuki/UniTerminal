using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// AssetManagerのテスト
    /// </summary>
    public class AssetManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            // テストごとにインスタンスをリセット
            AssetManager.ResetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            AssetManager.Instance.Registry.Clear();
            AssetManager.ResetInstance();
        }

        // ASMGR-001 シングルトン取得
        [Test]
        public void Instance_ReturnsSameInstance()
        {
            var instance1 = AssetManager.Instance;
            var instance2 = AssetManager.Instance;

            Assert.AreSame(instance1, instance2);
        }

        // ASMGR-002 インスタンスリセット
        [Test]
        public void ResetInstance_CreatesNewInstance()
        {
            var instance1 = AssetManager.Instance;
            AssetManager.ResetInstance();
            var instance2 = AssetManager.Instance;

            Assert.AreNotSame(instance1, instance2);
        }

        // ASMGR-010 プロバイダー登録
        [Test]
        public void RegisterProvider_AddsProvider()
        {
            var provider = new MockAssetProvider("MockProvider");
            AssetManager.Instance.RegisterProvider(provider);

            var retrieved = AssetManager.Instance.GetProvider("MockProvider");

            Assert.IsNotNull(retrieved);
            Assert.AreSame(provider, retrieved);
        }

        // ASMGR-011 nullプロバイダー登録
        [Test]
        public void RegisterProvider_NullProvider_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => AssetManager.Instance.RegisterProvider(null));
        }

        // ASMGR-012 プロバイダー上書き
        [Test]
        public void RegisterProvider_SameName_OverwritesProvider()
        {
            var provider1 = new MockAssetProvider("MockProvider");
            var provider2 = new MockAssetProvider("MockProvider");

            AssetManager.Instance.RegisterProvider(provider1);
            AssetManager.Instance.RegisterProvider(provider2);

            var retrieved = AssetManager.Instance.GetProvider("MockProvider");
            Assert.AreSame(provider2, retrieved);
        }

        // ASMGR-013 存在しないプロバイダー取得
        [Test]
        public void GetProvider_NonExisting_ReturnsNull()
        {
            var provider = AssetManager.Instance.GetProvider("NonExistent");
            Assert.IsNull(provider);
        }

        // ASMGR-020 利用可能プロバイダー一覧
        [Test]
        public void GetAvailableProviders_ReturnsOnlyAvailable()
        {
            var availableProvider = new MockAssetProvider("Available", true);
            var unavailableProvider = new MockAssetProvider("Unavailable", false);

            AssetManager.Instance.RegisterProvider(availableProvider);
            AssetManager.Instance.RegisterProvider(unavailableProvider);

            var providers = new System.Collections.Generic.List<IAssetProvider>(
                AssetManager.Instance.GetAvailableProviders());

            Assert.AreEqual(1, providers.Count);
            Assert.AreSame(availableProvider, providers[0]);
        }

        // ASMGR-030 アセット解決（ジェネリック）
        [Test]
        public void Resolve_RegisteredAsset_ReturnsAsset()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");
            var resolved = AssetManager.Instance.Resolve<Material>("TestMaterial");

            Assert.IsNotNull(resolved);
            Assert.AreEqual(material, resolved);

            Object.DestroyImmediate(material);
        }

        // ASMGR-031 アセット解決（インスタンスID）
        [Test]
        public void Resolve_ByInstanceId_ReturnsAsset()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");
            var specifier = $"#{material.GetInstanceID()}";
            var resolved = AssetManager.Instance.Resolve<Material>(specifier);

            Assert.IsNotNull(resolved);
            Assert.AreEqual(material, resolved);

            Object.DestroyImmediate(material);
        }

        // ASMGR-032 型不一致の解決
        [Test]
        public void Resolve_TypeMismatch_ReturnsNull()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");
            var resolved = AssetManager.Instance.Resolve<Texture2D>("TestMaterial");

            Assert.IsNull(resolved);

            Object.DestroyImmediate(material);
        }

        // ASMGR-033 存在しないアセット解決
        [Test]
        public void Resolve_NonExisting_ReturnsNull()
        {
            var resolved = AssetManager.Instance.Resolve<Material>("NonExistent");
            Assert.IsNull(resolved);
        }

        // ASMGR-034 アセット解決（Type指定版）
        [Test]
        public void Resolve_WithType_ReturnsAsset()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            AssetManager.Instance.Registry.Register(material, "test/path", "TestProvider");
            var resolved = AssetManager.Instance.Resolve("TestMaterial", typeof(Material));

            Assert.IsNotNull(resolved);
            Assert.AreEqual(material, resolved);

            Object.DestroyImmediate(material);
        }

        // ASMGR-040 アンロード
        [Test]
        public void Unload_RegisteredAsset_ReturnsTrue()
        {
            var provider = new MockAssetProvider("MockProvider");
            AssetManager.Instance.RegisterProvider(provider);

            var material = new Material(Shader.Find("Standard"));
            material.name = "TestMaterial";

            AssetManager.Instance.Registry.Register(material, "test/path", "MockProvider");
            var result = AssetManager.Instance.Unload("TestMaterial");

            Assert.IsTrue(result);
            Assert.AreEqual(0, AssetManager.Instance.Registry.Count);
            Assert.IsTrue(provider.ReleaseWasCalled);

            Object.DestroyImmediate(material);
        }

        // ASMGR-041 存在しないアセットのアンロード
        [Test]
        public void Unload_NonExisting_ReturnsFalse()
        {
            var result = AssetManager.Instance.Unload("NonExistent");
            Assert.IsFalse(result);
        }

        // ASMGR-050 LoadAsync成功
        [Test]
        public async Task LoadAsync_ValidKey_ReturnsEntry()
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = "LoadedMaterial";

            var provider = new MockAssetProvider("MockProvider");
            provider.AssetToReturn = material;
            AssetManager.Instance.RegisterProvider(provider);

            var entry = await AssetManager.Instance.LoadAsync<Material>(
                provider, "test/key", CancellationToken.None);

            Assert.IsNotNull(entry);
            Assert.AreEqual(material, entry.Asset);
            Assert.AreEqual("test/key", entry.Key);
            Assert.AreEqual("MockProvider", entry.ProviderName);

            Object.DestroyImmediate(material);
        }

        // ASMGR-051 LoadAsync失敗
        [Test]
        public async Task LoadAsync_FailedLoad_ReturnsNull()
        {
            var provider = new MockAssetProvider("MockProvider");
            provider.AssetToReturn = null;

            var entry = await AssetManager.Instance.LoadAsync<Material>(
                provider, "invalid/key", CancellationToken.None);

            Assert.IsNull(entry);
        }

        /// <summary>
        /// テスト用のモックプロバイダー
        /// </summary>
        private class MockAssetProvider : IAssetProvider
        {
            public string ProviderName { get; }
            public bool IsAvailable { get; }
            public Object AssetToReturn { get; set; }
            public bool ReleaseWasCalled { get; private set; }

            public MockAssetProvider(string name, bool isAvailable = true)
            {
                ProviderName = name;
                IsAvailable = isAvailable;
            }

            public Task<T> LoadAsync<T>(string key, CancellationToken ct) where T : Object
            {
                return Task.FromResult(AssetToReturn as T);
            }

            public Task<Object> LoadAsync(string key, System.Type assetType, CancellationToken ct)
            {
                return Task.FromResult(AssetToReturn);
            }

            public void Release(Object asset)
            {
                ReleaseWasCalled = true;
            }

            public System.Collections.Generic.IEnumerable<AssetInfo> Find(string pattern, System.Type assetType)
            {
                yield break;
            }

            public System.Collections.Generic.IEnumerable<AssetInfo> List(string path, System.Type assetType)
            {
                yield break;
            }
        }
    }
}
