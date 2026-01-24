using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// アセット管理の中央クラス。
    /// </summary>
    public class AssetManager
    {
        private static AssetManager instance;

        /// <summary>
        /// シングルトンインスタンス。
        /// </summary>
        public static AssetManager Instance => instance ??= new AssetManager();

        /// <summary>
        /// ロード済みアセットのレジストリ。
        /// </summary>
        public LoadedAssetRegistry Registry { get; } = new();

        /// <summary>
        /// 登録されているプロバイダー。
        /// </summary>
        private readonly Dictionary<string, IAssetProvider> providers = new();

        /// <summary>
        /// プロバイダーを登録します。
        /// </summary>
        /// <param name="provider">登録するプロバイダー</param>
        public void RegisterProvider(IAssetProvider provider)
        {
            if (provider == null)
                return;

            providers[provider.ProviderName] = provider;
        }

        /// <summary>
        /// プロバイダーを取得します。
        /// </summary>
        /// <param name="name">プロバイダー名</param>
        /// <returns>プロバイダー、見つからない場合はnull</returns>
        public IAssetProvider GetProvider(string name)
        {
            providers.TryGetValue(name, out var provider);
            return provider;
        }

        /// <summary>
        /// 利用可能なプロバイダー一覧を取得します。
        /// </summary>
        /// <returns>プロバイダーのリスト</returns>
        public IEnumerable<IAssetProvider> GetAvailableProviders()
        {
            foreach (var provider in providers.Values)
            {
                if (provider.IsAvailable)
                    yield return provider;
            }
        }

        /// <summary>
        /// 指定子でアセットを解決します。
        /// </summary>
        /// <typeparam name="T">アセットの型</typeparam>
        /// <param name="specifier">指定子（#instanceId, 名前, またはキー）</param>
        /// <returns>解決されたアセット、見つからない場合はnull</returns>
        public T Resolve<T>(string specifier) where T : UnityEngine.Object
        {
            if (Registry.TryResolve(specifier, out var entry))
            {
                if (entry.Asset is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// 指定子でアセットを解決します（型指定版）。
        /// </summary>
        /// <param name="specifier">指定子</param>
        /// <param name="assetType">アセットの型</param>
        /// <returns>解決されたアセット、見つからない場合はnull</returns>
        public UnityEngine.Object Resolve(string specifier, Type assetType)
        {
            if (Registry.TryResolve(specifier, out var entry))
            {
                if (assetType.IsAssignableFrom(entry.AssetType))
                    return entry.Asset;
            }
            return null;
        }

        /// <summary>
        /// アセットをロードしてレジストリに登録します。
        /// </summary>
        /// <typeparam name="T">アセットの型</typeparam>
        /// <param name="provider">使用するプロバイダー</param>
        /// <param name="key">アセットのキー</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロードされたアセットのエントリ</returns>
        public async Task<LoadedAssetEntry> LoadAsync<T>(IAssetProvider provider, string key, CancellationToken ct)
            where T : UnityEngine.Object
        {
            var asset = await provider.LoadAsync<T>(key, ct);
            return asset == null ? null : Registry.Register(asset, key, provider.ProviderName);
        }

        /// <summary>
        /// アセットをロードしてレジストリに登録します（型指定版）。
        /// </summary>
        /// <param name="provider">使用するプロバイダー</param>
        /// <param name="key">アセットのキー</param>
        /// <param name="assetType">アセットの型</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロードされたアセットのエントリ</returns>
        public async Task<LoadedAssetEntry> LoadAsync(IAssetProvider provider, string key, Type assetType, CancellationToken ct)
        {
            var asset = await provider.LoadAsync(key, assetType, ct);
            return asset == null ? null : Registry.Register(asset, key, provider.ProviderName);
        }

        /// <summary>
        /// アセットをアンロードしてレジストリから削除します。
        /// </summary>
        /// <param name="specifier">指定子</param>
        /// <returns>アンロードできた場合true</returns>
        public bool Unload(string specifier)
        {
            if (!Registry.TryResolve(specifier, out var entry))
                return false;

            var provider = GetProvider(entry.ProviderName);
            provider?.Release(entry.Asset);

            return Registry.Unregister(entry.InstanceId);
        }

        /// <summary>
        /// インスタンスをリセットします（テスト用）。
        /// </summary>
        public static void ResetInstance()
        {
            instance = null;
        }
    }
}
