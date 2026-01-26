#if UNITY_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// Addressables経由のアセットプロバイダー。
    /// </summary>
    public class AddressableAssetProvider : IAssetProvider
    {
        private readonly Dictionary<int, AsyncOperationHandle> handles = new();

        /// <summary>
        /// プロバイダー名。
        /// </summary>
        public string ProviderName => "Addressables";

        /// <summary>
        /// 利用可能かどうかを返します。
        /// </summary>
        public bool IsAvailable => true;

        /// <summary>
        /// アセットを非同期でロードします。
        /// </summary>
        /// <typeparam name="T">アセット型。</typeparam>
        /// <param name="key">アセットキー。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>ロード済みアセット。</returns>
        public async Task<T> LoadAsync<T>(string key, CancellationToken ct) where T : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            var handle = Addressables.LoadAssetAsync<T>(key);
            var asset = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && asset != null)
                handles[asset.GetInstanceID()] = handle;

            return asset;
        }

        /// <summary>
        /// 型指定でアセットを非同期ロードします。
        /// </summary>
        /// <param name="key">アセットキー。</param>
        /// <param name="assetType">アセット型。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>ロード済みアセット。</returns>
        public async Task<UnityEngine.Object> LoadAsync(string key, Type assetType, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
            var asset = await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && asset != null)
            {
                if (assetType.IsAssignableFrom(asset.GetType()))
                {
                    handles[asset.GetInstanceID()] = handle;
                    return asset;
                }
                Addressables.Release(handle);
            }

            return null;
        }

        /// <summary>
        /// ロード済みアセットを解放します。
        /// </summary>
        /// <param name="asset">解放対象アセット。</param>
        public void Release(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            var instanceId = asset.GetInstanceID();
            if (handles.TryGetValue(instanceId, out var handle))
            {
                Addressables.Release(handle);
                handles.Remove(instanceId);
            }
        }

        /// <summary>
        /// パターンに一致するアセット情報を取得します。
        /// </summary>
        /// <param name="pattern">検索パターン。</param>
        /// <param name="assetType">アセット型。</param>
        /// <returns>アセット情報列挙。</returns>
        public IEnumerable<AssetInfo> Find(string pattern, Type assetType = null)
        {
            var type = assetType ?? typeof(UnityEngine.Object);

            IList<IResourceLocation> locations = null;
            try
            {
                var handle = Addressables.LoadResourceLocationsAsync(type);
                handle.WaitForCompletion();
                locations = handle.Result;
                Addressables.Release(handle);
            }
            catch
            {
                yield break;
            }

            if (locations == null)
                yield break;

            Regex regex = null;
            if (!string.IsNullOrEmpty(pattern))
            {
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            }

            foreach (var location in locations)
            {
                var key = location.PrimaryKey;
                var name = System.IO.Path.GetFileNameWithoutExtension(key);

                if (regex != null && !regex.IsMatch(name) && !regex.IsMatch(key))
                    continue;

                yield return new AssetInfo
                {
                    Name = name,
                    Path = location.InternalId,
                    Key = key,
                    AssetType = location.ResourceType,
                    Size = 0,
                    ProviderName = ProviderName
                };
            }
        }

        /// <summary>
        /// 指定パスのアセット情報を列挙します。
        /// </summary>
        /// <param name="path">検索パス。</param>
        /// <param name="assetType">アセット型。</param>
        /// <returns>アセット情報列挙。</returns>
        public IEnumerable<AssetInfo> List(string path = null, Type assetType = null)
        {
            return Find(path, assetType);
        }

        /// <summary>
        /// ラベル一覧を取得します。
        /// </summary>
        /// <returns>ラベルのリスト</returns>
        public IEnumerable<string> GetLabels()
        {
            // Addressablesの内部APIを使用してラベルを取得
            // 注意: これは公式APIではないため、将来変更される可能性があります
            try
            {
                var locator = Addressables.ResourceLocators.FirstOrDefault();
                if (locator != null)
                    return locator.Keys.OfType<string>().Where(k => !k.Contains("/"));
            }
            catch
            {
                // エラーを無視
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// 全てのハンドルを解放します。
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var handle in handles.Values)
            {
                Addressables.Release(handle);
            }
            handles.Clear();
        }
    }
}
#endif
