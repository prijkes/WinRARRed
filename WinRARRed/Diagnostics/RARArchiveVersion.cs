using System;

namespace WinRARRed.Diagnostics
{
    [Flags]
    public enum RARArchiveVersion
    {
        /// <summary>
        /// RAR 4.x archive format.
        /// </summary>
        RAR4 = 0x01,

        /// <summary>
        /// RAR 5.0 archive format.
        /// </summary>
        RAR5 = 0x02
    }
}
