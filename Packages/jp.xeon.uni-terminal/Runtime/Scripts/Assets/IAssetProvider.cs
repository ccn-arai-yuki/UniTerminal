using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// アセット情報を表すクラス。
    /// </summary>
    public class AssetInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Key { get; set; }
        public Type AssetType { get; set; }
        public long Size { get; set; }
        public string ProviderName { get; set; }

        public override string ToString()
        {
            return $"{Name} ({AssetType?.Name ?? "Unknown"}) [{ProviderName}]";
        }
    }

    /// <summary>
    /// アセットプロバイダーの共通インターフェース。
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// プロバイダー名。
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// このプロバイダーが利用可能かどうか。
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// アセットをロードします。
        /// </summary>
        /// <typeparam name="T">アセットの型</typeparam>
        /// <param name="key">アセットのキー（パス、アドレス等）</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロードされたアセット</returns>
        Task<T> LoadAsync<T>(string key, CancellationToken ct) where T : UnityEngine.Object;

        /// <summary>
        /// アセットをロードします（型指定版）。
        /// </summary>
        /// <param name="key">アセットのキー</param>
        /// <param name="assetType">アセットの型</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>ロードされたアセット</returns>
        Task<UnityEngine.Object> LoadAsync(string key, Type assetType, CancellationToken ct);

        /// <summary>
        /// アセットをリリースします。
        /// </summary>
        /// <param name="asset">リリースするアセット</param>
        void Release(UnityEngine.Object asset);

        /// <summary>
        /// ロード可能なアセットを検索します。
        /// </summary>
        /// <param name="pattern">検索パターン（ワイルドカード対応）</param>
        /// <param name="assetType">アセットの型（nullで全型）</param>
        /// <returns>アセット情報のリスト</returns>
        IEnumerable<AssetInfo> Find(string pattern, Type assetType = null);

        /// <summary>
        /// ロード可能なアセット一覧を取得します。
        /// </summary>
        /// <param name="path">検索パス（プロバイダーにより意味が異なる）</param>
        /// <param name="assetType">アセットの型（nullで全型）</param>
        /// <returns>アセット情報のリスト</returns>
        IEnumerable<AssetInfo> List(string path = null, Type assetType = null);
    }
}
