using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands.Less
{
    /// <summary>
    /// lessコマンドの入力読み取り結果。
    /// </summary>
    public readonly struct ReadResult
    {
        /// <summary>
        /// 読み取った行。
        /// </summary>
        public List<string> Lines { get; }

        /// <summary>
        /// 入力元のファイル名。
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// エラーメッセージ。
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// エラー結果を生成します。
        /// </summary>
        /// <param name="error">エラーメッセージ。</param>
        public ReadResult(string error)
        {
            Lines = new();
            FileName = null;
            Error = error;
        }

        /// <summary>
        /// 読み取り結果を生成します。
        /// </summary>
        /// <param name="lines">読み取った行。</param>
        /// <param name="fileName">入力元のファイル名。</param>
        public ReadResult(List<string> lines, string fileName)
        {
            Lines = lines;
            FileName = fileName;
            Error = null;
        }

        /// <summary>
        /// エラーが存在するかどうか。
        /// </summary>
        public bool HasError => Error != null;

        /// <summary>
        /// 行が存在しないかどうか。
        /// </summary>
        public bool NoLines => Lines != null && Lines.Count == 0;
    }
}
