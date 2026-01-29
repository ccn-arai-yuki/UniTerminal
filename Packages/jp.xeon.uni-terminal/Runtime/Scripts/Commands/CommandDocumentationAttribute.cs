using System;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// コマンドのドキュメント情報を提供する属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CommandDocumentationAttribute : Attribute
    {
        /// <summary>
        /// コマンドのカテゴリ（file-operations, text-processing, unity-commands, utilities）
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// コマンドの書式（例: "ls [-a] [-l] [path...]"）
        /// </summary>
        public string Synopsis { get; set; } = "";

        /// <summary>
        /// コマンドの詳細説明
        /// </summary>
        public string LongDescription { get; set; } = "";

        /// <summary>
        /// 使用例（複数行は\nで区切る）
        /// </summary>
        public string Examples { get; set; } = "";

        /// <summary>
        /// 補足説明（複数行は\nで区切る）
        /// </summary>
        public string Notes { get; set; } = "";

        /// <summary>
        /// サブコマンド一覧（JSON形式: [{"name":"create","description":"..."},...]）
        /// </summary>
        public string SubCommands { get; set; } = "";

        /// <summary>
        /// 終了コード説明（JSON形式: [{"code":0,"description":"成功"},...]）
        /// </summary>
        public string ExitCodes { get; set; } = "";
    }
}
