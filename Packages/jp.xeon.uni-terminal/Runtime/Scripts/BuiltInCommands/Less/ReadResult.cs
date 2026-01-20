using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands.Less
{
    public class ReadResult
    {
        public List<string> Lines { get; }
        public string FileName { get; }
        public string Error { get; }

        public ReadResult(string error)
        {
            Error = error;
        }

        public ReadResult(List<string> lines, string fileName)
        {
            Lines = lines;
            FileName = fileName;
        }

        public bool HasError => Error != null;

        public bool NoLines => Lines != null && Lines.Count == 0;
    }
}