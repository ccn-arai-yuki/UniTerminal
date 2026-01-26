using System.Collections.Generic;
using NUnit.Framework;
using Xeon.UniTerminal.Common;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// TextMeshUtilityのテキスト整形に関するテスト
    /// </summary>
    public class TextMeshUtilityTests
    {
        // TXTMESH-001 マルチバイト混在文字列の改行
        [Test]
        public void WrapText_MixedMultibyteText_WrapsByDisplayWidth()
        {
            const string input = "abcあいうdef";
            const int maxCharsPerLine = 6;

            var lines = TextMeshUtility.WrapText(input, maxCharsPerLine);

            var expected = new List<string>
            {
                "abcあ",
                "いうde",
                "f",
            };

            CollectionAssert.AreEqual(expected, lines);
            Assert.AreEqual(input, string.Concat(lines));
        }
    }
}
