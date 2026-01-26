using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace XDebugger.Editor.Scripts
{
    /// <summary>
    /// スクリプトのインポート/移動時に文字コードをUTF-8へ自動変換するエディタ用ポストプロセッサ。
    /// 日本語コードの自動判定と変換を行い、Unityでの文字化けを防止します。
    /// </summary>
    public class ScriptImportPostProcessor : AssetPostprocessor
    {
        /// <summary>
        /// アセットインポート・移動・削除時に呼ばれるコールバック。
        /// .csファイルの文字コードをUTF-8へ変換します。
        /// </summary>
        /// <param name="importedAssets">インポートされたアセット</param>
        /// <param name="deletedAssets">削除されたアセット（変換処理では使用しません）</param>
        /// <param name="movedAssets">移動されたアセット</param>
        /// <param name="movedFromAssetPaths">移動元パス（変換処理では使用しません）</param>
        /// <remarks>
        /// 削除されたアセットや移動元パスは変換処理の対象外です。
        /// </remarks>
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // 対象となる.csファイルを抽出
            var targets = importedAssets.Where(path => Path.GetExtension(path) == ".cs")
                .Concat(movedAssets.Where(path => Path.GetExtension(path) == ".cs"));
            foreach (var path in targets)
            {
                var encoding = EncodeHelper.GetJpEncoding(path); // 文字コード判定
                if (encoding == null)
                {
                    Debug.LogError("Failed to get encoding");
                    continue;
                }
                if (encoding.EncodingName == Encoding.UTF8.EncodingName)
                {
                    continue; // 既にUTF-8なら何もしない
                }
                // 元の文字コードでファイル内容を読み込む
                string data;
                using (var sr = new StreamReader(path, encoding))
                {
                    data = sr.ReadToEnd();
                }
                // UTF-8で上書き保存
                using (var sw = new StreamWriter(path, false, Encoding.UTF8))
                {
                    sw.Write(data);
                }
                Debug.Log($"{path} is encoded {encoding.EncodingName} to UTF8");
            }
        }
    }
}
