using System.IO;
using System.Text;

namespace XDebugger.Editor.Scripts
{
    /// <summary>
    /// 文字コード判定・変換のヘルパークラス。
    /// 主に日本語ファイルの文字コード判定に利用。
    /// </summary>
    public static class EncodeHelper
    {
        /// <summary>
        /// 指定ファイルの日本語文字コードを判定して取得します。
        /// </summary>
        /// <param name="file">判定対象のファイルパス</param>
        /// <param name="maxSize">最大読み取りバイト数</param>
        /// <returns>判定されたEncoding。失敗時はnull。</returns>
        public static Encoding GetJpEncoding(string file, long maxSize = 50 * 1024)
        {
            try
            {
                if (!File.Exists(file)) return null;
                if (new FileInfo(file).Length == 0) return null;

                // ファイルのバイナリデータを最大maxSizeまで読み込む
                byte[] bytes;
                bool readAll = false;
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var size = fs.Length;
                    if (size <= maxSize)
                    {
                        bytes = new byte[size];
                        int readBytes = fs.Read(bytes, 0, (int)size);
                        if (readBytes < size) return null;
                        readAll = true;
                    }
                    else
                    {
                        bytes = new byte[maxSize];
                        int readBytes = fs.Read(bytes, 0, (int)maxSize);
                        if (readBytes < maxSize) return null;
                    }
                }
                // バイト配列から文字コード判定
                return GetJpEncoding(bytes, readAll);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// バイト配列から日本語文字コードを判定します。
        /// BOMや各種日本語コード（Shift_JIS, UTF-8, EUC-JP等）を判定。
        /// </summary>
        /// <param name="bytes">判定対象のバイト配列</param>
        /// <param name="readAll">全データ読み込み済みか</param>
        /// <returns>判定されたEncoding。失敗時はnull。</returns>
        private static Encoding GetJpEncoding(byte[] bytes, bool readAll = false)
        {
            int len = bytes.Length;

            // BOM判定
            if (len >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff) // UTF-16BE
                return Encoding.BigEndianUnicode;
            if (len >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe) // UTF-16LE
                return Encoding.Unicode;
            if (len >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) // UTF-8
                return new UTF8Encoding(true, true);
            if (len >= 3 && bytes[0] == 0x2b && bytes[1] == 0x2f && bytes[2] == 0x76) // UTF-7
                return Encoding.UTF7;
            if (len >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xfe && bytes[3] == 0xff) // UTF-32BE
                return new UTF32Encoding(true, true);
            if (len >= 4 && bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0x00 && bytes[3] == 0x00) // UTF-32LE
                return new UTF32Encoding(false, true);

            // ここから日本語コード（Shift_JIS, UTF-8, EUC-JP）の判定ロジック
            bool sjis = true;
            bool sjis2Ndbyte = false;
            bool sjisKana = false;
            bool sjisKanji = false;
            int counterSjis = 0;

            bool utf8 = true;
            bool utf8Multibyte = false;
            bool utf8KanaKanji = false;
            int counterUTF8 = 0;
            int counterUTF8Multibyte = 0;

            bool eucjp = true;
            bool eucjpMultibyte = false;
            bool eucjpKanaKanji = false;
            int counterEucjp = 0;
            int counterEucjpMultibyte = 0;

            for (int i = 0; i < len; i++)
            {
                byte b = bytes[i];

                //Shift_JIS判定
                if (sjis)
                {
                    if (!sjis2Ndbyte)
                    {
                        if (b == 0x0D || b == 0x0A || b == 0x09 || (0x20 <= b && b <= 0x7E))
                        {
                            counterSjis++;
                        }
                        else if ((0x81 <= b && b <= 0x9F) || (0xE0 <= b && b <= 0xFC))
                        {
                            sjis2Ndbyte = true;
                            if (0x82 <= b && b <= 0x83) sjisKana = true;
                            else if ((0x88 <= b && b <= 0x9F) || (0xE0 <= b && b <= 0xE3) || b == 0xE6 || b == 0xE7) sjisKanji = true;
                        }
                        else if (0xA1 <= b && b <= 0xDF) { /* 半角カナ */ }
                        else if (b <= 0x7F) { /* ASCII */ }
                        else { counterSjis = 0; sjis = false; }
                    }
                    else
                    {
                        if ((0x40 <= b && b <= 0x7E) || (0x80 <= b && b <= 0xFC))
                        {
                            if (sjisKana && (0x40 <= b && b <= 0xF1)) counterSjis += 2;
                            else if (sjisKanji && (0x40 <= b && b <= 0xFC && b != 0x7F)) counterSjis += 2;
                            sjis2Ndbyte = sjisKana = sjisKanji = false;
                        }
                        else { counterSjis = 0; sjis = false; }
                    }
                }
                //UTF-8判定
                if (utf8)
                {
                    if (!utf8Multibyte)
                    {
                        if (b == 0x0D || b == 0x0A || b == 0x09 || (0x20 <= b && b <= 0x7E)) counterUTF8++;
                        else if (0xC2 <= b && b <= 0xDF) { utf8Multibyte = true; counterUTF8Multibyte = 1; }
                        else if (0xE0 <= b && b <= 0xEF) { utf8Multibyte = true; counterUTF8Multibyte = 2; if (b == 0xE3 || (0xE4 <= b && b <= 0xE9)) utf8KanaKanji = true; }
                        else if (0xF0 <= b && b <= 0xF3) { utf8Multibyte = true; counterUTF8Multibyte = 3; }
                        else if (b <= 0x7F) { /* ASCII */ }
                        else { counterUTF8 = 0; utf8 = false; }
                    }
                    else
                    {
                        if (counterUTF8Multibyte > 0) { counterUTF8Multibyte--; if (b < 0x80 || 0xBF < b) { counterUTF8 = 0; utf8 = false; } }
                        if (utf8 && counterUTF8Multibyte == 0) { if (utf8KanaKanji) counterUTF8 += 3; utf8Multibyte = utf8KanaKanji = false; }
                    }
                }
                //EUC-JP判定
                if (eucjp)
                {
                    if (!eucjpMultibyte)
                    {
                        if (b == 0x0D || b == 0x0A || b == 0x09 || (0x20 <= b && b <= 0x7E)) counterEucjp++;
                        else if (b == 0x8E || (0xA1 <= b && b <= 0xA8) || b == 0xAD || (0xB0 <= b && b <= 0xFE)) { eucjpMultibyte = true; counterEucjpMultibyte = 1; if (b == 0xA4 || b == 0xA5 || (0xB0 <= b && b <= 0xEE)) eucjpKanaKanji = true; }
                        else if (b == 0x8F) { eucjpMultibyte = true; counterEucjpMultibyte = 2; }
                        else if (b <= 0x7F) { /* ASCII */ }
                        else { counterEucjp = 0; eucjp = false; }
                    }
                    else
                    {
                        if (counterEucjpMultibyte > 0) { counterEucjpMultibyte--; if (b < 0xA1 || 0xFE < b) { counterEucjp = 0; eucjp = false; } }
                        if (eucjp && counterEucjpMultibyte == 0) { if (eucjpKanaKanji) counterEucjp += 2; eucjpMultibyte = eucjpKanaKanji = false; }
                    }
                }
                //ISO-2022-JP
                if (b == 0x1B)
                {
                    if ((i + 2 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x40)
                        || (i + 2 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x42)
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x4A)
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x49)
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x42)
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x44)
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x4F)
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x51)
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x50)
                        || (i + 5 < len && bytes[i + 1] == 0x26 && bytes[i + 2] == 0x40 && bytes[i + 3] == 0x1B && bytes[i + 4] == 0x24 && bytes[i + 5] == 0x42))
                    {
                        return Encoding.GetEncoding(50220);//iso-2022-jp
                    }
                }
            }

            // すべて読み取った場合で、最後が多バイト文字の途中で終わっている場合は判定NG
            if (readAll)
            {
                if (sjis && sjis2Ndbyte)
                {
                    sjis = false;
                }

                if (utf8 && utf8Multibyte)
                {
                    utf8 = false;
                }

                if (eucjp && eucjpMultibyte)
                {
                    eucjp = false;
                }
            }

            if (sjis || utf8 || eucjp)
            {
                //日本語らしさの最大値確認
                int maxValue = counterEucjp;
                if (counterSjis > maxValue) maxValue = counterSjis;
                if (counterUTF8 > maxValue) maxValue = counterUTF8;
                //文字コード判定
                if (maxValue == counterUTF8) return new UTF8Encoding(false, true);//utf8
                if (maxValue == counterSjis) return Encoding.GetEncoding(932);//ShiftJIS
                else return Encoding.GetEncoding(51932);//EUC-JP
            }
            else
            {
                return null;
            }
        }
    }
}
