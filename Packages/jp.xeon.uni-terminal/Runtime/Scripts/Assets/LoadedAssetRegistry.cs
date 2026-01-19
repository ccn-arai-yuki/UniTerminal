using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// ロード済みアセットのエントリ。
    /// </summary>
    public class LoadedAssetEntry
    {
        public UnityEngine.Object Asset { get; set; }
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public Type AssetType { get; set; }
        public string ProviderName { get; set; }
        public DateTime LoadedAt { get; set; }

        public override string ToString()
        {
            return $"{Name} #{InstanceId} ({AssetType?.Name ?? "Unknown"}) [{ProviderName}]";
        }
    }

    /// <summary>
    /// ロード済みアセットを管理するレジストリ。
    /// </summary>
    public class LoadedAssetRegistry
    {
        private readonly Dictionary<int, LoadedAssetEntry> entriesByInstanceId = new();
        private readonly Dictionary<string, List<LoadedAssetEntry>> entriesByName = new();

        /// <summary>
        /// ロード済みアセット数。
        /// </summary>
        public int Count => entriesByInstanceId.Count;

        /// <summary>
        /// アセットを登録します。
        /// </summary>
        /// <param name="asset">登録するアセット</param>
        /// <param name="key">アセットのキー（パス、アドレス等）</param>
        /// <param name="providerName">プロバイダー名</param>
        /// <returns>登録されたエントリ</returns>
        public LoadedAssetEntry Register(UnityEngine.Object asset, string key, string providerName)
        {
            if (asset == null)
                return null;

            var instanceId = asset.GetInstanceID();

            // 既に登録済みの場合は既存エントリを返す
            if (entriesByInstanceId.TryGetValue(instanceId, out var existing))
                return existing;

            var entry = new LoadedAssetEntry
            {
                Asset = asset,
                InstanceId = instanceId,
                Name = asset.name,
                Key = key,
                AssetType = asset.GetType(),
                ProviderName = providerName,
                LoadedAt = DateTime.Now
            };

            entriesByInstanceId[instanceId] = entry;

            if (!entriesByName.TryGetValue(asset.name, out var nameList))
            {
                nameList = new List<LoadedAssetEntry>();
                entriesByName[asset.name] = nameList;
            }
            nameList.Add(entry);

            return entry;
        }

        /// <summary>
        /// アセットを登録解除します。
        /// </summary>
        /// <param name="asset">登録解除するアセット</param>
        /// <returns>登録解除されたらtrue</returns>
        public bool Unregister(UnityEngine.Object asset)
        {
            if (asset == null)
                return false;

            return Unregister(asset.GetInstanceID());
        }

        /// <summary>
        /// インスタンスIDでアセットを登録解除します。
        /// </summary>
        /// <param name="instanceId">インスタンスID</param>
        /// <returns>登録解除されたらtrue</returns>
        public bool Unregister(int instanceId)
        {
            if (!entriesByInstanceId.TryGetValue(instanceId, out var entry))
                return false;

            entriesByInstanceId.Remove(instanceId);

            if (entriesByName.TryGetValue(entry.Name, out var nameList))
            {
                nameList.RemoveAll(e => e.InstanceId == instanceId);
                if (nameList.Count == 0)
                    entriesByName.Remove(entry.Name);
            }

            return true;
        }

        /// <summary>
        /// インスタンスIDでアセットを取得します。
        /// </summary>
        /// <param name="instanceId">インスタンスID</param>
        /// <returns>アセットエントリ、見つからない場合はnull</returns>
        public LoadedAssetEntry GetByInstanceId(int instanceId)
        {
            entriesByInstanceId.TryGetValue(instanceId, out var entry);
            return entry;
        }

        /// <summary>
        /// 名前でアセットを取得します（同名が1つの場合のみ成功）。
        /// </summary>
        /// <param name="name">アセット名</param>
        /// <param name="entry">取得されたエントリ</param>
        /// <returns>一意に特定できた場合true</returns>
        public bool TryGetByName(string name, out LoadedAssetEntry entry)
        {
            entry = null;

            if (!entriesByName.TryGetValue(name, out var nameList))
                return false;

            if (nameList.Count != 1)
                return false;

            entry = nameList[0];
            return true;
        }

        /// <summary>
        /// 名前でアセットを取得します（複数可）。
        /// </summary>
        /// <param name="name">アセット名</param>
        /// <returns>アセットエントリのリスト</returns>
        public IReadOnlyList<LoadedAssetEntry> GetByName(string name)
        {
            if (entriesByName.TryGetValue(name, out var nameList))
                return nameList;
            return Array.Empty<LoadedAssetEntry>();
        }

        /// <summary>
        /// 指定子でアセットを解決します。
        /// </summary>
        /// <param name="specifier">指定子（#instanceId, 名前, またはキー）</param>
        /// <param name="entry">解決されたエントリ</param>
        /// <returns>解決できた場合true</returns>
        public bool TryResolve(string specifier, out LoadedAssetEntry entry)
        {
            entry = null;

            if (string.IsNullOrEmpty(specifier))
                return false;

            // インスタンスID指定 (#12345形式)
            if (specifier.StartsWith("#") && int.TryParse(specifier.Substring(1), out int instanceId))
            {
                entry = GetByInstanceId(instanceId);
                return entry != null;
            }

            // 名前で検索（同名が1つの場合のみ）
            if (TryGetByName(specifier, out entry))
                return true;

            // キーで検索
            entry = entriesByInstanceId.Values.FirstOrDefault(e => e.Key == specifier);
            return entry != null;
        }

        /// <summary>
        /// 型でフィルタリングしたアセット一覧を取得します。
        /// </summary>
        /// <param name="assetType">アセットの型（nullで全て）</param>
        /// <returns>アセットエントリのリスト</returns>
        public IEnumerable<LoadedAssetEntry> GetAll(Type assetType = null)
        {
            var entries = entriesByInstanceId.Values;

            if (assetType != null)
                entries = entries.Where(e => assetType.IsAssignableFrom(e.AssetType));

            return entries.OrderBy(e => e.Name);
        }

        /// <summary>
        /// パターンで検索します。
        /// </summary>
        /// <param name="pattern">検索パターン（ワイルドカード対応）</param>
        /// <param name="assetType">アセットの型（nullで全て）</param>
        /// <returns>マッチしたアセットエントリのリスト</returns>
        public IEnumerable<LoadedAssetEntry> Find(string pattern, Type assetType = null)
        {
            var regexPattern = "^" + Regex.Escape(pattern ?? "*")
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var entries = entriesByInstanceId.Values;

            if (assetType != null)
                entries = entries.Where(e => assetType.IsAssignableFrom(e.AssetType));

            return entries
                .Where(e => regex.IsMatch(e.Name) || (e.Key != null && regex.IsMatch(e.Key)))
                .OrderBy(e => e.Name);
        }

        /// <summary>
        /// 全てのエントリをクリアします。
        /// </summary>
        public void Clear()
        {
            entriesByInstanceId.Clear();
            entriesByName.Clear();
        }
    }
}
