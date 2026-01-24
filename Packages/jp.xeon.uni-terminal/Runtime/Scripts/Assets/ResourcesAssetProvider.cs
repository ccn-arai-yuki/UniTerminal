using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// Resources経由のアセットプロバイダー。
    /// 注意: Resources APIはUnityが非推奨としています。
    /// 可能であればAddressablesの使用を推奨します。
    /// </summary>
    public class ResourcesAssetProvider : IAssetProvider
    {
        public string ProviderName => "Resources";

        public bool IsAvailable => true;

        public Task<T> LoadAsync<T>(string key, CancellationToken ct) where T : Object
        {
            ct.ThrowIfCancellationRequested();

            var asset = Resources.Load<T>(key);
            return Task.FromResult(asset);
        }

        public Task<Object> LoadAsync(string key, Type assetType, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var asset = Resources.Load(key, assetType);
            return Task.FromResult(asset);
        }

        public void Release(Object asset)
        {
            if (asset != null)
                Resources.UnloadAsset(asset);
        }

        public IEnumerable<AssetInfo> Find(string pattern, Type assetType = null)
        {
            // Resourcesでは実行時にファイル一覧を取得する標準的な方法がない
            // Resources.FindObjectsOfTypeAllで既にロード済みのものから検索
            var type = assetType ?? typeof(Object);
            var objects = Resources.FindObjectsOfTypeAll(type);

            Regex regex = null;
            if (!string.IsNullOrEmpty(pattern))
            {
                var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            }

            foreach (var obj in objects)
            {
                // エディタ専用オブジェクトをスキップ
                if (obj.hideFlags.HasFlag(HideFlags.NotEditable) || obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                    continue;

                if (regex != null && !regex.IsMatch(obj.name))
                    continue;

                yield return new AssetInfo
                {
                    Name = obj.name,
                    Path = null,
                    Key = null,
                    AssetType = obj.GetType(),
                    Size = 0,
                    ProviderName = ProviderName
                };
            }
        }

        public IEnumerable<AssetInfo> List(string path = null, Type assetType = null)
        {
            // Resourcesでは指定パス以下のアセット一覧を取得する方法がない
            // 全てロード済みのものを返す
            return Find(null, assetType);
        }

        /// <summary>
        /// 未使用のアセットをアンロードします。
        /// </summary>
        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
