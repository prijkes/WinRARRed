using System;

namespace WinRARRed
{
    public static class Log
    {
        public static event EventHandler<string>? Logged;

        public static void Write(object? sender, string text)
        {
            Logged?.Invoke(sender, text);
        }
    }
}
