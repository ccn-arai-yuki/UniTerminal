#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// AssetDatabase経由のアセットプロバイダー（エディタ専用）。
    /// </summary>
    public class AssetDatabaseProvider : IAssetProvider
    {
        public string ProviderName => "AssetDatabase";

        public bool IsAvailable => Application.isEditor;

        public Task<T> LoadAsync<T>(string key, CancellationToken ct) where T : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            var asset = AssetDatabase.LoadAssetAtPath<T>(key);
            return Task.FromResult(asset);
        }

        public Task<UnityEngine.Object> LoadAsync(string key, Type assetType, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var asset = AssetDatabase.LoadAssetAtPath(key, assetType);
            return Task.FromResult(asset);
        }

        public void Release(UnityEngine.Object asset)
        {
            // AssetDatabaseでロードしたアセットは自動管理されるため、
            // 明示的なリリースは不要
        }

        public IEnumerable<AssetInfo> Find(string pattern, Type assetType = null)
        {
            // AssetDatabase.FindAssetsを使用して検索
            var filter = BuildSearchFilter(pattern, assetType);
            var guids = AssetDatabase.FindAssets(filter);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);

                if (assetType != null && !assetType.IsAssignableFrom(type))
                    continue;

                yield return new AssetInfo
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Path = path,
                    Key = guid,
                    AssetType = type,
                    Size = GetFileSize(path),
                    ProviderName = ProviderName
                };
            }
        }

        public IEnumerable<AssetInfo> List(string path = null, Type assetType = null)
        {
            string[] searchFolders = null;

            if (!string.IsNullOrEmpty(path))
            {
                if (AssetDatabase.IsValidFolder(path))
                    searchFolders = new[] { path };
                else
                    yield break;
            }

            var filter = assetType != null ? $"t:{assetType.Name}" : "";
            var guids = searchFolders != null
                ? AssetDatabase.FindAssets(filter, searchFolders)
                : AssetDatabase.FindAssets(filter);

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

                yield return new AssetInfo
                {
                    Name = Path.GetFileNameWithoutExtension(assetPath),
                    Path = assetPath,
                    Key = guid,
                    AssetType = type,
                    Size = GetFileSize(assetPath),
                    ProviderName = ProviderName
                };
            }
        }

        /// <summary>
        /// GUIDからアセットパスを取得します。
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>アセットパス</returns>
        public string GetAssetPath(string guid)
        {
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        /// <summary>
        /// インスタンスIDからアセットパスを取得します。
        /// </summary>
        /// <param name="instanceId">インスタンスID</param>
        /// <returns>アセットパス</returns>
        public string GetAssetPathFromInstanceId(int instanceId)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj == null)
                return null;

            return AssetDatabase.GetAssetPath(obj);
        }

        private string BuildSearchFilter(string pattern, Type assetType)
        {
            var parts = new List<string>();

            if (assetType != null)
                parts.Add($"t:{assetType.Name}");

            if (!string.IsNullOrEmpty(pattern))
            {
                // ワイルドカードをAssetDatabase形式に変換
                // *と?はそのままAssetDatabaseが解釈する
                parts.Add(pattern);
            }

            return string.Join(" ", parts);
        }

        private long GetFileSize(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                    return new FileInfo(fullPath).Length;
            }
            catch
            {
                // エラーを無視
            }
            return 0;
        }
    }
}
#endif
