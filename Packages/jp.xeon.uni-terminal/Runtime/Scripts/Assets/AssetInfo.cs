using System;

namespace Xeon.UniTerminal.Assets
{
    /// <summary>
    /// アセット情報を表すクラス
    /// </summary>
    public class AssetInfo
    {
        /// <summary>
        /// アセット名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// アセットパス
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// プロバイダー固有のキー
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// アセット型
        /// </summary>
        public Type AssetType { get; set; }

        /// <summary>
        /// アセットサイズ（バイト）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// プロバイダー名
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// 表示用文字列を生成します
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({AssetType?.Name ?? "Unknown"}) [{ProviderName}]";
        }
    }
}
