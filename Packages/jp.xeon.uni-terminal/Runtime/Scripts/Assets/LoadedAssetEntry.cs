using System;

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
}