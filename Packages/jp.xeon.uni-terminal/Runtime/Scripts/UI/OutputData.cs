namespace Xeon.UniTerminal
{
    public struct OutputData
    {
        public string Message { get; }
        public bool IsError { get; }

        public OutputData(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }
    }
}