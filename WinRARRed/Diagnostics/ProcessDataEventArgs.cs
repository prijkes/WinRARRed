using System;

namespace WinRARRed.Diagnostics
{
    public class ProcessDataEventArgs(string? data) : EventArgs
    {
        public string? Data { get; private set; } = data;

        public bool Error { get; private set; }

        public ProcessDataEventArgs(string? data, bool error) : this(data)
        {
            Error = error;
        }
    }
}
