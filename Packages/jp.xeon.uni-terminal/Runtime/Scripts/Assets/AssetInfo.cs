using System;

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
}