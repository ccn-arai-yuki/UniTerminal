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
    /// AssetDatabase経由のアセットプロバイダー（エディタ専用）
    /// </summary>
    public class AssetDatabaseProvider : IAssetProvider
    {
        /// <summary>
        /// プロバイダー名
        /// </summary>
        public string ProviderName => "AssetDatabase";

        /// <summary>
        /// 利用可能かどうかを返します
        /// </summary>
        public bool IsAvailable => Application.isEditor;

        /// <summary>
        /// アセットを非同期でロードします
        /// </summary>
        /// <typeparam name="T">アセット型</typeparam>
        /// <param name="key">アセットキー</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロード済みアセット</returns>
        public Task<T> LoadAsync<T>(string key, CancellationToken ct) where T : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            var asset = AssetDatabase.LoadAssetAtPath<T>(key);
            return Task.FromResult(asset);
        }

        /// <summary>
        /// 型指定でアセットを非同期ロードします
        /// </summary>
        /// <param name="key">アセットキー</param>
        /// <param name="assetType">アセット型</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロード済みアセット</returns>
        public Task<UnityEngine.Object> LoadAsync(string key, Type assetType, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var asset = AssetDatabase.LoadAssetAtPath(key, assetType);
            return Task.FromResult(asset);
        }

        /// <summary>
        /// AssetDatabase経由でロードしたアセットの解放処理
        /// </summary>
        /// <param name="asset">解放対象アセット</param>
        public void Release(UnityEngine.Object asset)
        {
            // AssetDatabaseでロードしたアセットは自動管理されるため、
            // 明示的なリリースは不要
        }

        /// <summary>
        /// パターンに一致するアセット情報を取得します
        /// </summary>
        /// <param name="pattern">検索パターン</param>
        /// <param name="assetType">アセット型</param>
        /// <returns>アセット情報列挙</returns>
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

        /// <summary>
        /// 指定パス配下のアセット情報を列挙します
        /// </summary>
        /// <param name="path">検索パス</param>
        /// <param name="assetType">アセット型</param>
        /// <returns>アセット情報列挙</returns>
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
        /// GUIDからアセットパスを取得します
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>アセットパス</returns>
        public string GetAssetPath(string guid)
        {
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        /// <summary>
        /// インスタンスIDからアセットパスを取得します
        /// </summary>
        /// <param name="instanceId">インスタンスID</param>
        /// <returns>アセットパス</returns>
        public string GetAssetPathFromInstanceId(int instanceId)
        {
#if UNITY_6000_3_OR_NEWER
            var obj = EditorUtility.EntityIdToObject(instanceId);
#else
            var obj = EditorUtility.InstanceIDToObject(instanceId);
#endif
            return obj == null ? null : AssetDatabase.GetAssetPath(obj);
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
