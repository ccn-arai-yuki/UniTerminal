using System.Text.RegularExpressions;

namespace Xeon.UniTerminal.BuiltInCommands
{
    public readonly struct FindParams
    {
        public string pattern { get; }
        private readonly bool ignoreCase;
        private readonly FindFileType fileType;

        public FindParams(string pattern, bool ignoreCase, FindFileType fileType)
        {
            this.pattern = pattern;
            this.ignoreCase = ignoreCase;
            this.fileType = fileType;
        }

        public bool MatchType(bool isFile, bool isDirectory)
        {
            if (fileType is FindFileType.All)
                return true;
            if (fileType is FindFileType.File && isFile)
                return true;
            if (fileType is FindFileType.Directory && isDirectory)
                return true;
            return false;
        }

        public RegexOptions RegexOption => ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
    }
}