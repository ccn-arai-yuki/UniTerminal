using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands.Less
{
    public readonly struct ReadResult
    {
        public List<string> Lines { get; }
        public string FileName { get; }
        public string Error { get; }

        public ReadResult(string error)
        {
            Lines = new();
            FileName = null;
            Error = error;
        }

        public ReadResult(List<string> lines, string fileName)
        {
            Lines = lines;
            FileName = fileName;
            Error = null;
        }

        public bool HasError => Error != null;

        public bool NoLines => Lines != null && Lines.Count == 0;
    }
}